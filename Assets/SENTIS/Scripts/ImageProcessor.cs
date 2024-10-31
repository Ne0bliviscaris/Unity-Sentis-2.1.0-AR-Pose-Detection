using UnityEngine;

/// Handles image preprocessing for ML model
namespace Sentis
{
    public class ImageProcessor
    {
        private readonly int targetSize;

        public ImageProcessor(int targetSize)
        {
            this.targetSize = targetSize;
        }

        public Texture2D ScaleImage(Texture2D sourceImage)
        {
            var rt = RenderTexture.GetTemporary(targetSize, targetSize);
            Graphics.Blit(sourceImage, rt);

            var scaledImage = new Texture2D(targetSize, targetSize);
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
