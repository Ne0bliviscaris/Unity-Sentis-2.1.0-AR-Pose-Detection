// WebCameraProvider.cs
using System;
using System.Collections;
using UnityEngine;

namespace Sentis
{
    /// <summary>
    /// Handles webcam input for pose detection
    /// </summary>
    public class WebCameraProvider : MonoBehaviour, ICameraProvider
    {
        [SerializeField]
        private bool isWebGL = false;

        private WebCamTexture webCamTexture;
        private Texture2D currentFrame;
        private int width = 640;
        private int height = 480;

        public bool IsInitialized => webCamTexture != null;

        private void Start()
        {
            isWebGL = Application.platform == RuntimePlatform.WebGLPlayer;
            if (isWebGL)
            {
                Debug.Log("WebGL platform detected");
                StartCoroutine(RequestWebGLPermission());
            }
        }

        private IEnumerator RequestWebGLPermission()
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);

            if (Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                Initialize();
                StartCapture();
            }
            else
            {
                Debug.LogError("WebCam permission denied");
            }
        }

        public void Initialize()
        {
            if (WebCamTexture.devices.Length == 0)
            {
                Debug.LogError("No camera detected");
                return;
            }

            // Explicit size for WebGL
            webCamTexture = new WebCamTexture(width, height, 30);
            currentFrame = new Texture2D(width, height, TextureFormat.RGBA32, false);
        }

        public void StartCapture()
        {
            if (!IsInitialized)
                Initialize();
            if (webCamTexture != null && !webCamTexture.isPlaying)
            {
                webCamTexture.Play();
            }
        }

        public void StopCapture()
        {
            if (webCamTexture != null && webCamTexture.isPlaying)
            {
                webCamTexture.Stop();
            }
        }

        public Texture2D GetCurrentFrame()
        {
            if (!IsInitialized || !webCamTexture.isPlaying)
                return null;

            try
            {
                // Check actual dimensions
                if (
                    currentFrame.width != webCamTexture.width
                    || currentFrame.height != webCamTexture.height
                )
                {
                    currentFrame = new Texture2D(
                        webCamTexture.width,
                        webCamTexture.height,
                        TextureFormat.RGBA32,
                        false
                    );
                }

                currentFrame.SetPixels32(webCamTexture.GetPixels32());
                currentFrame.Apply();
                return currentFrame;
            }
            catch (Exception e)
            {
                Debug.LogError($"Frame capture failed: {e.Message}");
                return null;
            }
        }

        private void OnDestroy()
        {
            StopCapture();
            if (webCamTexture != null)
                webCamTexture.Stop();
        }
    }
}
