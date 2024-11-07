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
        private Texture2D scaledImage;

        private const int MODEL_INPUT_SIZE = 640;
        private const float PROCESS_INTERVAL = 0.0f; // No interval

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
            using var outputTensor = ExecuteModel(inputTensor);

            var keypoints = outputProcessor.ProcessModelOutput(outputTensor);

            if (keypoints != null && keypoints.Length > 0)
            {
                // Uncomment to debug keypoints
                // ImageProcessorHelper.DebugKeypoints(keypoints); ////////////////////////////////////////////
                keypointVisualizer.UpdateKeypoints(keypoints);
            }
            else
            {
                Debug.LogWarning("No valid keypoints detected");
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
