// KeypointUtils.cs
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Sentis
{
    public static class KeypointUtils
    {
        public static Vector2 GetPixelCoordinates(
            Vector2 pixelPos,
            int textureWidth,
            int textureHeight
        )
        {
            // Zaokrąglamy współrzędne pikseli do liczb całkowitych
            return new Vector2(Mathf.RoundToInt(pixelPos.x), Mathf.RoundToInt(pixelPos.y));
        }

        /// Validate if position is within texture bounds
        public static bool IsWithinTextureBounds(
            Vector2 pixelPos,
            int textureWidth,
            int textureHeight
        )
        {
            return pixelPos.x >= 0
                && pixelPos.x < textureWidth
                && pixelPos.y >= 0
                && pixelPos.y < textureHeight;
        }

        /// Draw circle on texture at specified position
        public static void DrawCircle(Texture2D texture, Vector2 center, float radius, Color color)
        {
            int centerX = Mathf.RoundToInt(center.x);
            int centerY = Mathf.RoundToInt(center.y);
            int radiusInt = Mathf.RoundToInt(radius);
            int radiusSquared = radiusInt * radiusInt;

            for (int i = -radiusInt; i <= radiusInt; i++)
            {
                for (int j = -radiusInt; j <= radiusInt; j++)
                {
                    if (i * i + j * j <= radiusSquared)
                    {
                        int px = centerX + i;
                        int py = centerY + j;

                        if (
                            IsWithinTextureBounds(
                                new Vector2(px, py),
                                texture.width,
                                texture.height
                            )
                        )
                        {
                            texture.SetPixel(px, py, color);
                        }
                    }
                }
            }
        }

        /// Validate keypoint visualization inputs
        public static bool ValidateVisualizationInput(RawImage targetImage, KeyPoint[] keypoints)
        {
            if (targetImage == null || keypoints == null)
            {
                Debug.LogWarning("Target image or keypoints are null");
                return false;
            }
            if (targetImage.texture == null)
            {
                Debug.LogWarning("Target image texture is null");
                return false;
            }

            // Debugowanie wartości confidence
            // Debug.Log(
            //     $"Confidence range: {keypoints.Min(k => k.Confidence)} - {keypoints.Max(k => k.Confidence)}"
            // );

            return true;
        }
    }
}
