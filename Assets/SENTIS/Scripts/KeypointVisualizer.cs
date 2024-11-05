// KeypointVisualizer.cs
using UnityEngine;
using UnityEngine.UI;

namespace Sentis
{
    /// Handles drawing keypoints on camera preview
    public class KeypointVisualizer : MonoBehaviour
    {
        [SerializeField, Tooltip("Size of keypoint circles")]
        private float pointSize = 10f; // Zmniejszony rozmiar punktu

        [SerializeField, Tooltip("Color for keypoints")]
        private Color pointColor = Color.green;

        [SerializeField, Tooltip("Reference to RawImage for drawing")]
        private RawImage targetImage;

        [SerializeField, Tooltip("Minimum confidence threshold")]
        private float confidenceThreshold = 0.5f;

        private Texture2D drawingTexture;

        public void DrawKeypoints(KeyPoint[] keypoints)
        {
            if (targetImage == null || keypoints == null)
            {
                Debug.LogWarning("Target image or keypoints are null");
                return;
            }

            // Create or recreate drawing texture if needed
            if (
                drawingTexture == null
                || drawingTexture.width != targetImage.texture.width
                || drawingTexture.height != targetImage.texture.height
            )
            {
                if (drawingTexture != null)
                    Destroy(drawingTexture);

                drawingTexture = new Texture2D(
                    targetImage.texture.width,
                    targetImage.texture.height,
                    TextureFormat.RGBA32,
                    false
                );
                Debug.Log($"Created new texture: {drawingTexture.width}x{drawingTexture.height}");
            }

            // Copy background
            Graphics.CopyTexture(targetImage.texture, drawingTexture);

            // Draw keypoints
            for (int i = 0; i < keypoints.Length; i++)
            {
                if (keypoints[i].Confidence > 0)
                {
                    // Normalizuj pozycję do zakresu [0,1]
                    Vector2 originalPos = keypoints[i].Position;
                    Vector2 normalizedPos = new Vector2(
                        originalPos.x / drawingTexture.width,
                        originalPos.y / drawingTexture.height
                    );

                    Debug.Log(
                        $"Keypoint {i}: Original: {originalPos}, Normalized: {normalizedPos}, Confidence: {keypoints[i].Confidence}"
                    );

                    // Sprawdź, czy znormalizowane wartości są w zakresie [0,1]
                    if (
                        normalizedPos.x >= 0
                        && normalizedPos.x <= 1
                        && normalizedPos.y >= 0
                        && normalizedPos.y <= 1
                    )
                    {
                        DrawPoint(normalizedPos, pointSize, pointColor);
                    }
                    else
                    {
                        Debug.LogWarning($"Keypoint {i} position out of range: {normalizedPos}");
                    }
                }
            }

            drawingTexture.Apply();
            targetImage.texture = drawingTexture;
        }

        private void DrawPoint(Vector2 position, float size, Color color)
        {
            // Przekształć współrzędne z zakresu [0,1] na piksele
            int x = Mathf.RoundToInt(position.x * drawingTexture.width);
            int y = Mathf.RoundToInt(position.y * drawingTexture.height);

            // Rysuj punkt jako koło
            int radius = Mathf.RoundToInt(size);
            int radiusSquared = radius * radius;

            for (int i = -radius; i <= radius; i++)
            {
                for (int j = -radius; j <= radius; j++)
                {
                    if (i * i + j * j <= radiusSquared)
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

        private void OnDestroy()
        {
            if (drawingTexture != null)
                Destroy(drawingTexture);
        }
    }
}
