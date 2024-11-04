// OutputUtils.cs
using System;
using Unity.Collections;
using Unity.Sentis;
using UnityEngine;

namespace Sentis
{
    public static class OutputUtils
    {
        /// Validates the length of the data array.
        public static bool ValidateDataArrayLength(NativeArray<float> dataArray, int expectedLength)
        {
            if (dataArray.Length < expectedLength)
            {
                Debug.LogError(
                    $"Invalid data array length. Expected: {expectedLength}, Got: {dataArray.Length}"
                );
                return false;
            }
            return true;
        }

        /// Clears the previous keypoints.
        public static void ClearKeypoints(KeyPoint[] keypoints, int numKeypoints)
        {
            for (int i = 0; i < numKeypoints; i++)
            {
                keypoints[i] = new KeyPoint(Vector2.zero, 0f);
            }
        }

        /// Extracts keypoints from the data array.
        public static void ExtractKeypoints(
            NativeArray<float> dataArray,
            KeyPoint[] keypoints,
            int numKeypoints,
            float confidenceThreshold
        )
        {
            for (int i = 0; i < numKeypoints; i++)
            {
                float x = dataArray[i * 3];
                float y = dataArray[i * 3 + 1];
                float conf = dataArray[i * 3 + 2];

                if (conf >= confidenceThreshold)
                {
                    keypoints[i] = new KeyPoint(new Vector2(x, y), conf);
                }
            }
        }

        /// Downloads tensor data to NativeArray.
        public static NativeArray<float> DownloadTensorData(Tensor<float> outputTensor)
        {
            Debug.Log("Downloading tensor data to NativeArray");
            return outputTensor.DownloadToNativeArray();
        }

        /// Checks if the output tensor is valid.
        public static bool IsOutputTensorValid(Tensor<float> outputTensor)
        {
            if (outputTensor == null || outputTensor.shape.length == 0)
            {
                Debug.LogError("Output tensor is null or empty");
                return false;
            }
            return true;
        }

        /// Prepares the output tensor for processing.
        public static void PrepareOutputTensor(
            ref Tensor currentOutputTensor,
            Tensor<float> outputTensor
        )
        {
            currentOutputTensor?.Dispose();
            currentOutputTensor = outputTensor;

            // Ensure all pending operations are completed
            outputTensor.CompleteAllPendingOperations();
        }

        /// Debugs tensor information.
        public static void DebugTensorInfo(Tensor tensor, NativeTensorArray array)
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
    }
}
