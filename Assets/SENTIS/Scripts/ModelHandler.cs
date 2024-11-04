using System;
using Unity.Sentis;
using UnityEngine;

/// Main class for ML model handling
namespace Sentis
{
    public class ModelHandler : MonoBehaviour, IDisposable
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
        private const int IMAGE_SIZE = 640;
        private bool disposed = false;
        private float lastProcessTime;
        private const float PROCESS_INTERVAL = 0.1f; // Zmniejszono do 2 razy na sekundę

        private void Start()
        {
            imageProcessor = new ImageProcessor(IMAGE_SIZE);
            tensorConverter = new TensorConverter(IMAGE_SIZE);
            outputProcessor = new OutputProcessor();
            InitializeModel();

            if (cameraProvider != null)
            {
                cameraProvider.Initialize();
                cameraProvider.StartCapture();
            }
            if (keypointVisualizer == null)
            {
                Debug.LogError("KeypointVisualizer not assigned!");
                return;
            }
            Debug.Log("KeypointVisualizer found and assigned");
        }

        private void InitializeModel()
        {
            try
            {
                var runtimeModel = ModelLoader.Load(modelAsset);

                // Najprostsza inicjalizacja workera w Sentis 2.1.0
                worker = new Worker(runtimeModel, BackendType.CPU);

                if (worker == null)
                {
                    Debug.LogError("Failed to create worker");
                    return;
                }

                Debug.Log(
                    $"Model initialized with CPU backend. Input shape: {runtimeModel.inputs[0].shape}"
                );
            }
            catch (Exception e)
            {
                Debug.LogError($"Model initialization failed: {e.Message}\nStack: {e.StackTrace}");
            }
        }

        public void ProcessImage(Texture2D image)
        {
            if (worker == null || disposed)
                return;

            try
            {
                // 1. Przygotowanie i konwersja
                var scaledImage = imageProcessor.ScaleImage(image);
                using var inputTensor = tensorConverter.ImageToTensor(scaledImage);

                // 2. Wykonanie modelu
                worker.Schedule(inputTensor);

                // 3. Pobranie i rzutowanie tensora wyjściowego
                using var outputTensor = worker.PeekOutput() as Tensor<float>;
                if (outputTensor == null)
                {
                    Debug.LogError("Output tensor is not of type Tensor<float>");
                    return;
                }

                // 4. Przetwarzanie wyniku
                var keypoints = outputProcessor.ProcessOutput(outputTensor);

                if (keypoints != null && keypoints.Length > 0)
                {
                    // Debugowanie pierwszych kluczowych punktów
                    for (int i = 0; i < Math.Min(3, keypoints.Length); i++)
                    {
                        Debug.Log(
                            $"Keypoint {i}: Position={keypoints[i].Position}, Confidence={keypoints[i].Confidence}"
                        );
                    }

                    keypointVisualizer.DrawKeypoints(keypoints);
                }
                else
                {
                    Debug.LogWarning("No valid keypoints detected");
                }

                if (scaledImage != image)
                    UnityEngine.Object.Destroy(scaledImage);
            }
            catch (Exception e)
            {
                Debug.LogError($"Processing failed: {e.Message}\nStack: {e.StackTrace}");
            }
        }

        private void UpdateCameraPreview()
        {
            if (cameraProvider != null && cameraProvider.IsInitialized)
            {
                var frame = cameraProvider.GetCurrentFrame();
                if (frame != null && previewImage != null)
                {
                    previewImage.texture = frame;
                }
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
