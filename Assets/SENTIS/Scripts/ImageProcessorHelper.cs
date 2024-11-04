// ImageProcessorHelper.cs
using Sentis;
using Unity.Sentis;
using UnityEngine;

namespace Sentis
{
    public static class ImageProcessorHelper
    {
        /// Processes the output tensor.
        public static KeyPoint[] ProcessOutput(
            OutputProcessor outputProcessor,
            Tensor<float> outputTensor
        )
        {
            return outputProcessor.ProcessOutput(outputTensor);
        }

        /// Debugs the first few keypoints.
        public static void DebugKeypoints(KeyPoint[] keypoints, int count = 3)
        {
            for (int i = 0; i < Mathf.Min(count, keypoints.Length); i++)
            {
                Debug.Log(
                    $"Keypoint {i}: Position={keypoints[i].Position}, Confidence={keypoints[i].Confidence}"
                );
            }
        }
    }
}
