using UnityEngine;

// KeyPoint.cs - struktura reprezentująca pojedynczy keypoint
namespace Sentis
{
    public struct KeyPoint
    {
        public Vector2 Position; // x,y coordinates
        public float Confidence; // detection confidence

        public KeyPoint(Vector2 position, float confidence)
        {
            Position = position;
            Confidence = confidence;
        }
    }
}
