using System;
using Unity.Sentis;
using UnityEngine;

/// Processes model output tensor into keypoints
namespace Sentis
{
    public class OutputProcessor
    {
        private const int NUM_KEYPOINTS = 17;
        private readonly float confidenceThreshold;

        public OutputProcessor(float confidenceThreshold = 0.5f)
        {
            this.confidenceThreshold = confidenceThreshold;
        }

        private void DebugTensorInfo(Tensor tensor, NativeTensorArray array)
        {
            Debug.Log($"Tensor info:");
            Debug.Log($"- Shape: {tensor.shape}");
            Debug.Log($"- Data type: {tensor.dataType}");
            Debug.Log($"- Length: {array.Length}");

            // Print first few values to check data structure
            Debug.Log("First 6 values:");
            for (int i = 0; i < Math.Min(6, array.Length); i++)
            {
                Debug.Log($"[{i}]: {array.Get<float>(i)}");
            }
        }

        public KeyPoint[] ProcessOutput(Tensor outputTensor)
        {
            var keypoints = new KeyPoint[NUM_KEYPOINTS];
            outputTensor.CompleteAllPendingOperations();

            var clonedTensor = outputTensor.ReadbackAndClone();
            var tensorData = clonedTensor.dataOnBackend as CPUTensorData;

            if (tensorData == null)
            {
                Debug.LogError("Failed to get tensor data");
                return keypoints;
            }

            var nativeArray = tensorData.array;

            // Add debug info first
            DebugTensorInfo(clonedTensor, nativeArray);

            // Check array bounds before processing
            if (nativeArray.Length < NUM_KEYPOINTS * 3)
            {
                Debug.LogError(
                    $"Tensor data too small. Expected: {NUM_KEYPOINTS * 3}, Got: {nativeArray.Length}"
                );
                return keypoints;
            }

            // Process each keypoint (x,y,confidence triplets)
            for (int i = 0; i < NUM_KEYPOINTS; i++)
            {
                float x = nativeArray.Get<float>(i * 3);
                float y = nativeArray.Get<float>(i * 3 + 1);
                float conf = nativeArray.Get<float>(i * 3 + 2);

                if (conf >= confidenceThreshold)
                {
                    keypoints[i] = new KeyPoint(new Vector2(x, y), conf);
                }
            }

            return keypoints;
        }
    }
}
