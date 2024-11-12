using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Sentis;
using UnityEngine;

/// Main class for ML model handling
namespace Sentis
{
    public class ModelHandler : MonoBehaviour
    {
        [SerializeField, Tooltip("YOLO pose detection model asset")]
        private ModelAsset modelAsset;

        [SerializeField, Tooltip("Reference to camera input provider component")]
        private WebCameraProvider cameraProvider;

        [SerializeField, Tooltip("Raw image component for camera preview")]
        private UnityEngine.UI.RawImage previewImage;

        [SerializeField]
        private Camera mainCamera; // Dodaj referencję do kamery

        [SerializeField]
        private KeypointVisualizer keypointVisualizer;

        private Worker worker;
        private ImageProcessor imageProcessor;
        private TensorConverter tensorConverter;
        private OutputProcessor outputProcessor;
        private bool disposed = false;
        private float lastProcessTime;

        private const int MODEL_INPUT_SIZE = 640;
        private const float PROCESS_INTERVAL = 0.0f; // No interval

        private int NUM_KEYPOINTS = 17;
        private float confidenceThreshold = 0.2f;

        private void Start()
        {
            if (!HelperMethods.ValidateComponents(modelAsset, cameraProvider, previewImage))
            {
                enabled = false;
                return;
            }
            // Use properties instead of direct field access
            keypointVisualizer.TargetCamera = mainCamera;
            keypointVisualizer.CameraPreview = previewImage;

            HelperMethods.InitializeProcessors(
                out imageProcessor,
                out tensorConverter,
                out outputProcessor,
                MODEL_INPUT_SIZE
            );
            worker = HelperMethods.InitializeModel(modelAsset);
            HelperMethods.InitializeCamera(cameraProvider);
        }

        public void ProcessImageWithModel(Texture2D image)
        {
            if (disposed)
                return;

            using var inputTensor = PrepareAndConvertImage(image);
            var outputTensors = ExecuteModel(inputTensor);

            if (outputTensors != null)
            {
                try
                {
                    var keypoints = outputProcessor.ProcessModelOutput(outputTensors[0]);

                    if (keypoints != null && keypoints.Length > 0)
                    {
                        keypointVisualizer.UpdateKeypoints(keypoints);
                    }
                    else
                    {
                        Debug.LogWarning("No valid keypoints detected");
                    }
                }
                finally
                {
                    // Dispose tensors manually
                    foreach (var tensor in outputTensors)
                    {
                        tensor?.Dispose();
                    }
                }
            }
        }

        /// Prepares and converts the image to a tensor.
        private Tensor<float> PrepareAndConvertImage(Texture2D image)
        {
            var scaledImage = imageProcessor.ScaleImage(image);

            var inputTensor = tensorConverter.ImageToTensor(scaledImage);
            if (scaledImage != image)
                UnityEngine.Object.Destroy(scaledImage);
            return inputTensor;
        }

        /// Executes the model and returns the output tensor.
        private Tensor<float>[] ExecuteModel(Tensor inputTensor)
        {
            try
            {
                worker.Schedule(inputTensor);
                var rawOutput = worker.PeekOutput(0) as Tensor<float>;
                if (rawOutput == null)
                {
                    Debug.LogError("Failed to get model output");
                    return null;
                }

                rawOutput.CompleteAllPendingOperations();

                var detections = NonMaxSuppression(rawOutput, confidenceThreshold, 0.45f);

                if (detections.Count == 0)
                {
                    Debug.Log("No valid keypoints detected");
                    return null;
                }

                var bestDetection = detections[0];
                var landmarksTensor = new Tensor<float>(new TensorShape(1, NUM_KEYPOINTS * 3));

                for (int kp = 0; kp < NUM_KEYPOINTS; kp++)
                {
                    landmarksTensor[0, kp * 3] = bestDetection[kp * 3];
                    landmarksTensor[0, kp * 3 + 1] = bestDetection[kp * 3 + 1];
                    landmarksTensor[0, kp * 3 + 2] = bestDetection[kp * 3 + 2];
                }

                rawOutput.Dispose();
                return new[] { landmarksTensor };
            }
            catch (Exception e)
            {
                Debug.LogError($"Error executing model: {e.Message}\nStackTrace: {e.StackTrace}");
                return null;
            }
        }

        /// Performs Non-Maximum Suppression (NMS) on the model output.
        private List<float[]> NonMaxSuppression(
            Tensor<float> prediction,
            float confThres = 0.25f,
            float iouThres = 0.45f
        )
        {
            var output = new List<float[]>();

            for (int i = 0; i < prediction.shape[2]; i++)
            {
                float confidence = prediction[0, 4, i];
                if (confidence < confThres)
                    continue;

                var box = new float[56];
                for (int j = 0; j < 56; j++)
                {
                    box[j] = prediction[0, j, i];
                }

                output.Add(box);
            }
            Debug.Log($"Detections before NMS: {output.Count}");

            output = output.OrderByDescending(x => x[4]).ToList();

            var selected = new List<float[]>();
            while (output.Count > 0)
            {
                var best = output[0];
                selected.Add(best);
                output.RemoveAt(0);

                output = output.Where(box => IoU(best, box) < iouThres).ToList();
            }
            Debug.Log($"Detections after NMS: {selected.Count}");

            return selected;
        }

        /// Calculates Intersection over Union (IoU) for two bounding boxes.
        private float IoU(float[] boxA, float[] boxB)
        {
            float x1 = Mathf.Max(boxA[0], boxB[0]);
            float y1 = Mathf.Max(boxA[1], boxB[1]);
            float x2 = Mathf.Min(boxA[2], boxB[2]);
            float y2 = Mathf.Min(boxA[3], boxB[3]);

            float intersection = Mathf.Max(0, x2 - x1) * Mathf.Max(0, y2 - y1);
            float areaA = (boxA[2] - boxA[0]) * (boxA[3] - boxA[1]);
            float areaB = (boxB[2] - boxB[0]) * (boxB[3] - boxB[1]);

            return intersection / (areaA + areaB - intersection);
        }

        private void UpdateCameraPreview()
        {
            var frame = cameraProvider.GetCurrentFrame();
            if (frame != null && previewImage != null)
            {
                previewImage.texture = frame;
            }
        }

        private void Update()
        {
            UpdateCameraPreview();
            // Sprawdź czy minął wymagany czas
            float elapsedTime = Time.time - lastProcessTime;
            if (elapsedTime < PROCESS_INTERVAL)
                return;

            var frame = cameraProvider.GetCurrentFrame();
            if (frame != null)
            {
                ProcessImageWithModel(frame);
                lastProcessTime = Time.time;
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                worker?.Dispose();

                if (imageProcessor != null)
                    ((IDisposable)imageProcessor).Dispose();

                if (tensorConverter != null)
                    ((IDisposable)tensorConverter).Dispose();

                if (outputProcessor != null)
                    ((IDisposable)outputProcessor).Dispose();

                disposed = true;
            }
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}
