using System;
using Sentis;
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
        private const int IMAGE_SIZE = 640;

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
        }

        private void InitializeModel()
        {
            try
            {
                var runtimeModel = ModelLoader.Load(modelAsset);
                // Zmiana na CPU dla WebGL
                if (Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    worker = new Worker(runtimeModel, BackendType.CPU);
                    Debug.Log("Using CPU backend for WebGL");
                }
                else
                {
                    worker = new Worker(runtimeModel, BackendType.GPUCompute);
                    Debug.Log("Using GPU backend");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Model initialization failed: {e.Message}");
            }
        }

        public void ProcessImage(Texture2D image)
        {
            try
            {
                var scaledImage = imageProcessor.ScaleImage(image);
                var inputTensor = tensorConverter.ImageToTensor(scaledImage);

                using (inputTensor) // Dispose input tensor after use
                {
                    worker.Schedule(inputTensor);
                    var outputTensor = worker.PeekOutput();

                    using (outputTensor) // Dispose output tensor after use
                    {
                        var keypoints = outputProcessor.ProcessOutput(outputTensor);

                        if (keypointVisualizer != null)
                        {
                            keypointVisualizer.DrawKeypoints(keypoints);
                        }
                    }
                }

                // Clean up scaled image if needed
                if (scaledImage != image)
                {
                    UnityEngine.Object.Destroy(scaledImage);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Processing failed: {e.Message}");
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
            if (cameraProvider != null && cameraProvider.IsInitialized)
            {
                var frame = cameraProvider.GetCurrentFrame();
                if (frame != null)
                {
                    ProcessImage(frame);
                }
            }
        }
    }
}
