// KeypointVisualizer.cs
using UnityEngine;
using UnityEngine.UI;

namespace Sentis
{
    /// <summary>
    /// Handles drawing keypoints on camera preview
    /// </summary>
    public class KeypointVisualizer : MonoBehaviour
    {
        [SerializeField, Tooltip("Size of keypoint circles")]
        private float pointSize = 50f;

        [SerializeField, Tooltip("Color for keypoints")]
        private Color pointColor = Color.green;

        [SerializeField, Tooltip("Reference to RawImage for drawing")]
        private RawImage targetImage;

        private Texture2D drawingTexture;

        public void DrawKeypoints(KeyPoint[] keypoints)
        {
            if (targetImage == null || keypoints == null)
                return;

            // Create drawing texture if needed
            if (
                drawingTexture == null
                || drawingTexture.width != targetImage.texture.width
                || drawingTexture.height != targetImage.texture.height
            )
            {
                drawingTexture = new Texture2D(
                    targetImage.texture.width,
                    targetImage.texture.height,
                    TextureFormat.RGBA32,
                    false
                );
            }

            // Copy background
            Graphics.CopyTexture(targetImage.texture, drawingTexture);

            // Draw keypoints
            foreach (var keypoint in keypoints)
            {
                DrawPoint(keypoint.Position, pointSize, pointColor);
            }

            drawingTexture.Apply();
            targetImage.texture = drawingTexture;
        }

        private void DrawPoint(Vector2 position, float size, Color color)
        {
            int x = Mathf.RoundToInt(position.x * drawingTexture.width);
            int y = Mathf.RoundToInt(position.y * drawingTexture.height);

            for (int i = -Mathf.RoundToInt(size); i <= size; i++)
            {
                for (int j = -Mathf.RoundToInt(size); j <= size; j++)
                {
                    if (i * i + j * j <= size * size)
                    {
                        int px = x + i;
                        int py = y + j;
                        if (
                            px >= 0
                            && px < drawingTexture.width
                            && py >= 0
                            && py < drawingTexture.height
                        )
                        {
                            drawingTexture.SetPixel(px, py, color);
                        }
                    }
                }
            }
        }
    }
}
