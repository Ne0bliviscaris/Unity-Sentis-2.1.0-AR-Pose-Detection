using System;
using Unity.Sentis;
using UnityEngine;

namespace Sentis
{
    public static class HelperMethods
    {
        /// Validates required components.
        public static bool ValidateComponents(
            ModelAsset modelAsset,
            WebCameraProvider cameraProvider,
            UnityEngine.UI.RawImage previewImage
        )
        {
            if (modelAsset == null)
            {
                Debug.LogError("Model asset not assigned!");
                return false;
            }

            if (cameraProvider == null)
            {
                Debug.LogError("Camera provider not assigned!");
                return false;
            }

            if (previewImage == null)
            {
                Debug.LogError("Preview image not assigned!");
                return false;
            }
            return true;
        }

        /// Initializes processors.
        public static void InitializeProcessors(
            out ImageProcessor imageProcessor,
            out TensorConverter tensorConverter,
            out OutputProcessor outputProcessor,
            int imageSize
        )
        {
            imageProcessor = new ImageProcessor(imageSize);
            tensorConverter = new TensorConverter(imageSize);
            outputProcessor = new OutputProcessor();
            // Debug.Log("Processors initialized successfully");
        }

        /// Initializes ML model and creates worker
        public static Worker InitializeModel(ModelAsset modelAsset)
        {
            try
            {
                var runtimeModel = ModelLoader.Load(modelAsset);
                var worker = new Worker(runtimeModel, BackendType.CPU);

                if (worker == null)
                {
                    Debug.LogError("Failed to create worker");
                    return null;
                }

                // Debug.Log(
                //     $"Model initialized with CPU backend. Input shape: {runtimeModel.inputs[0].shape}"
                // );
                return worker;
            }
            catch (Exception e)
            {
                Debug.LogError($"Model initialization failed: {e.Message}\nStack: {e.StackTrace}");
                return null;
            }
        }

        /// Initializes and starts camera capture
        public static bool InitializeCamera(WebCameraProvider cameraProvider)
        {
            if (cameraProvider == null)
                return false;

            try
            {
                cameraProvider.Initialize();
                cameraProvider.StartCapture();
                // Debug.Log("Camera initialized and started");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Camera initialization failed: {e.Message}");
                return false;
            }
        }
    }
}
