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
                    Vector2 pos = keypoints[i].Position;

                    // Przekształć pozycję z zakresu [0,1] na piksele
                    Vector2 pixelPos = new Vector2(
                        pos.x * drawingTexture.width,
                        pos.y * drawingTexture.height
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
