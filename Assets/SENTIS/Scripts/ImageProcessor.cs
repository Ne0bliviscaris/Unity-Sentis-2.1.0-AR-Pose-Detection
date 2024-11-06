using System;
using UnityEngine;

/// Handles image preprocessing for ML model
namespace Sentis
{
    public class ImageProcessor : IDisposable
    {
        private readonly int targetSize;
        private Texture2D resizedImage;
        private RenderTexture renderTexture;
        private bool disposed = false;

        public ImageProcessor(int targetSize)
        {
            this.targetSize = targetSize;
        }

        public void Dispose()
        {
            if (!disposed)
            {
                if (resizedImage != null)
                    UnityEngine.Object.Destroy(resizedImage);

                if (renderTexture != null)
                {
                    renderTexture.Release();
                    UnityEngine.Object.Destroy(renderTexture);
                }

                disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        ~ImageProcessor()
        {
            Dispose();
        }

        /// Scales the source image to the target size.
        public Texture2D ScaleImage(Texture2D sourceImage)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(ImageProcessor));

            var rt = RenderTexture.GetTemporary(targetSize, targetSize);

            Graphics.Blit(sourceImage, rt);

            var scaledImage = new Texture2D(targetSize, targetSize, TextureFormat.RGBA32, false); // No mipmaps
            var prevActive = RenderTexture.active;
            RenderTexture.active = rt;
            scaledImage.ReadPixels(new Rect(0, 0, targetSize, targetSize), 0, 0);
            scaledImage.Apply();
            RenderTexture.active = prevActive;

            RenderTexture.ReleaseTemporary(rt);

            return scaledImage;
        }
    }
}
