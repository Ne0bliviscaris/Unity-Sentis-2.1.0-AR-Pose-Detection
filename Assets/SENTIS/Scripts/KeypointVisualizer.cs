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
        private float confidenceThreshold = 0.2f;

        // scale factor for keypoint positions
        [SerializeField, Tooltip("Scale factor for keypoint positions")]
        private float scaleFactor = 5f; // Zwiększamy skalę punktów

        [SerializeField, Tooltip("Offset for X coordinates")]
        private float offsetX = 0.2f; // Przesunięcie w poziomie

        [SerializeField, Tooltip("Offset for Y coordinates")]
        private float offsetY = 0.2f; // Przesunięcie w pionie

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
            Debug.Log(
                $"Drawing texture dimensions: {drawingTexture.width}x{drawingTexture.height}"
            );

            for (int i = 0; i < keypoints.Length; i++)
            {
                Vector2 normalizedPos = keypoints[i].Position;
                float confidence = keypoints[i].Confidence;
                string keypointName = ((KeypointName)i).ToString();

                // Skaluj i przesuń współrzędne
                Vector2 adjustedPos = new Vector2(
                    (normalizedPos.x * scaleFactor) + offsetX,
                    (normalizedPos.y * scaleFactor) + offsetY
                );

                // Zapewnij, że punkty mieszczą się w zakresie 0-1
                adjustedPos = new Vector2(
                    Mathf.Clamp01(adjustedPos.x),
                    Mathf.Clamp01(adjustedPos.y)
                );

                if (confidence > confidenceThreshold)
                {
                    Vector2 pixelPos = new Vector2(
                        adjustedPos.x * drawingTexture.width,
                        adjustedPos.y * drawingTexture.height
                    );

                    if (
                        KeypointUtils.IsWithinTextureBounds(
                            pixelPos,
                            drawingTexture.width,
                            drawingTexture.height
                        )
                    )
                    {
                        KeypointUtils.DrawCircle(drawingTexture, pixelPos, pointSize, pointColor);
                        Debug.Log(
                            $"Drawing {keypointName}: Adjusted={adjustedPos}, Pixels={pixelPos}, Confidence={confidence:F3}"
                        );
                    }
                }
            }
        }

        private void ApplyChanges()
        {
            drawingTexture.Apply();
            targetImage.texture = drawingTexture;
        }

        private void OnDestroy()
        {
            if (drawingTexture != null)
                Destroy(drawingTexture);
        }
    }
}
