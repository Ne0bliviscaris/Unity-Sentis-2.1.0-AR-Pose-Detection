using System;
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
        private float confidenceThreshold = 0.5f;

        private void Start()
        {
            if (!HelperMethods.ValidateComponents(modelAsset, cameraProvider, previewImage))
            {
                enabled = false;
                return;
            }

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
                    return null;

                rawOutput.CompleteAllPendingOperations();
                // Debug tensor shape
                // Debug.Log($"Raw output shape: [{string.Join(", ", rawOutput.shape)}]");

                // Znajdź najlepszą detekcję (uproszczone NMS)
                float bestScore = float.MinValue;
                int bestIdx = 0;

                // Sprawdź confidence score dla każdej detekcji
                for (int i = 0; i < rawOutput.shape[2]; i++)
                {
                    float confidence = rawOutput[0, 4, i];
                    confidence = Mathf.Clamp01(confidence);
                    if (confidence > bestScore)
                    {
                        bestScore = confidence;
                        bestIdx = i;
                    }
                }

                // Jeśli najlepsza detekcja ma za niski score, zwróć null
                if (bestScore < confidenceThreshold)
                {
                    Debug.Log($"No valid keypoints detected");
                    return null;
                }
                else
                {
                    Debug.Log($"Best detection: index={bestIdx}, confidence={bestScore:F3}");
                }

                // Skopiuj keypoints tylko dla najlepszej detekcji
                var landmarksTensor = new Tensor<float>(new TensorShape(1, NUM_KEYPOINTS * 3));
                int offset = 5;

                for (int kp = 0; kp < NUM_KEYPOINTS; kp++)
                {
                    // Get raw values
                    float x = rawOutput[0, offset + kp * 3, bestIdx];
                    float y = rawOutput[0, offset + kp * 3 + 1, bestIdx];
                    float conf = rawOutput[0, offset + kp * 3 + 2, bestIdx];

                    // Normalize confidence
                    conf = Mathf.Clamp01(conf);

                    // Debug values
                    if (kp == 0) // Log first keypoint as sample
                    {
                        Debug.Log($"Keypoint 0: x={x:F3}, y={y:F3}, conf={conf:F3}");
                    }

                    landmarksTensor[0, kp * 3] = x;
                    landmarksTensor[0, kp * 3 + 1] = y;
                    landmarksTensor[0, kp * 3 + 2] = conf;
                }

                rawOutput.Dispose();
                return new[] { landmarksTensor };
            }
            catch (Exception e)
            {
                Debug.LogError($"Error: {e.Message}");
                return null;
            }
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
