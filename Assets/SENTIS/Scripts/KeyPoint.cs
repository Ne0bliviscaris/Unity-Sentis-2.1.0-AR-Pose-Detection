using UnityEngine;

// KeyPoint.cs - struktura reprezentująca pojedynczy keypoint
namespace Sentis
{
    public struct KeyPoint
    {
        public Vector2 Position { get; private set; }
        public float Confidence { get; private set; }

        public KeyPoint(Vector2 position, float confidence)
        {
            Position = position;
            Confidence = confidence;
        }
    }
}
