// ImageProcessorHelper.cs
using UnityEngine;

namespace Sentis
{
    public static class ImageProcessorHelper
    {
        /// Debugs the first few keypoints.
        public static void DebugKeypoints(KeyPoint[] keypoints, int count = 3)
        {
            for (int i = 0; i < Mathf.Min(count, keypoints.Length); i++)
            {
                var keypointName = (KeypointName)i;
                Debug.Log(
                    $"Keypoint {i} {keypointName}: Position={keypoints[i].Position}, Confidence={keypoints[i].Confidence}"
                );
            }
        }
    }
}
