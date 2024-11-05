// KeypointVisualizer.cs
using System;
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
            if (!KeypointUtils.ValidateVisualizationInput(targetImage, keypoints))
                return;

            if (!PrepareTexture())
                return;

            Graphics.CopyTexture(targetImage.texture, drawingTexture);
            DrawAllKeypoints(keypoints);
            ApplyChanges();
        }

        private bool PrepareTexture()
        {
            try
            {
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
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to prepare texture: {e.Message}");
                return false;
            }
        }

        private void DrawAllKeypoints(KeyPoint[] keypoints)
        {
            for (int i = 0; i < keypoints.Length; i++)
            {
                if (keypoints[i].Confidence > confidenceThreshold)
                {
                    // Debug.Log dla sprawdzenia wartości
                    Debug.Log(
                        $"Keypoint {i}: Raw position={keypoints[i].Position}, Confidence={keypoints[i].Confidence}"
                    );

                    Vector2 pixelPos = KeypointUtils.GetPixelCoordinates(
                        keypoints[i].Position, // Używamy bezpośrednio znormalizowanej pozycji
                        drawingTexture.width,
                        drawingTexture.height
                    );

                    // Debug.Log($"Keypoint {i}: Pixel position={pixelPos}");

                    if (
                        KeypointUtils.IsWithinTextureBounds(
                            pixelPos,
                            drawingTexture.width,
                            drawingTexture.height
                        )
                    )
                    {
                        KeypointUtils.DrawCircle(drawingTexture, pixelPos, pointSize, pointColor);
                    }
                }
            }
        }

        private void ApplyChanges()
        {
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
