// KeypointUtils.cs
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Sentis
{
    public static class KeypointUtils
    {
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
    }
}
