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
        private const int MODEL_INPUT_SIZE = 640;
        private bool disposed = false;
        private float lastProcessTime;

        [SerializeField]
        private float PROCESS_INTERVAL = 0.3f; // Zmniejszono do 2 razy na sekundę

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
            if (worker == null)
            {
                enabled = false;
                return;
            }

            if (!HelperMethods.InitializeCamera(cameraProvider))
            {
                enabled = false;
                return;
            }
        }

        public void ProcessImage(Texture2D image)
        {
            if (worker == null || disposed)
                return;

            try
            {
                using var inputTensor = PrepareAndConvertImage(image);
                using var outputTensor = ExecuteModel(inputTensor);

                var keypoints = ProcessModelOutput(outputTensor);

                if (keypoints != null && keypoints.Length > 0)
                {
                    // Uncomment to debug keypoints
                    // ImageProcessorHelper.DebugKeypoints(keypoints);
                    keypointVisualizer.DrawKeypoints(keypoints);
                }
                else
                {
                    Debug.LogWarning("No valid keypoints detected");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Processing failed: {e.Message}\nStack: {e.StackTrace}");
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
        private Tensor<float> ExecuteModel(Tensor inputTensor)
        {
            worker.Schedule(inputTensor);
            return worker.PeekOutput() as Tensor<float>;
        }

        /// Processes the output tensor and returns keypoints.
        private KeyPoint[] ProcessModelOutput(Tensor<float> outputTensor)
        {
            return outputProcessor.ProcessOutput(outputTensor);
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
            if (Time.time - lastProcessTime < PROCESS_INTERVAL)
                return;

            if (cameraProvider != null && cameraProvider.IsInitialized)
            {
                var frame = cameraProvider.GetCurrentFrame();
                if (frame != null)
                {
                    ProcessImage(frame);
                    lastProcessTime = Time.time;
                }
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

        public void SetVisualizer(KeypointVisualizer visualizer)
        {
            keypointVisualizer = visualizer;
            Debug.Log($"Visualizer set: {(visualizer != null ? "yes" : "no")}");
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}
