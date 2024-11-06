// OutputUtils.cs
using System;
using System.Linq;
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
            if (!ValidateDataArrayLength(dataArray, numKeypoints * 3))
                return;

            Debug.Log($"Data array length: {dataArray.Length}");
            Debug.Log("Raw data array values:");
            for (int i = 0; i < Math.Min(30, dataArray.Length); i += 3)
            {
                KeypointName keypointName = (KeypointName)(i / 3);
                Debug.Log(
                    $"Index {i / 3} {keypointName}: x={dataArray[i]}, y={dataArray[i + 1]}, conf={dataArray[i + 2]}"
                );
            }

            for (int i = 0; i < numKeypoints; i++)
            {
                int xIndex = i * 3;
                int yIndex = i * 3 + 1;
                int confIndex = i * 3 + 2;

                float x = dataArray[xIndex];
                float y = dataArray[yIndex];
                float conf = dataArray[confIndex];

                KeypointName keypointName = (KeypointName)i;

                // Normalize coordinates if needed (if they're not already in 0-1 range)
                x = Mathf.Clamp01(x / 640f); // Assuming 640 is model input size
                y = Mathf.Clamp01(y / 640f);

                if (conf >= confidenceThreshold)
                {
                    keypoints[i] = new KeyPoint(new Vector2(x, y), conf);
                }
                else
                {
                    keypoints[i] = new KeyPoint(Vector2.zero, 0f);
                }
            }
        }

        // Debug raw tensor data
        public static void DebugRawTensorData(NativeArray<float> dataArray, int numKeypoints)
        {
            Debug.Log("Raw tensor data:");
            for (int i = 0; i < Math.Min(numKeypoints * 3, dataArray.Length); i += 3)
            {
                Debug.Log(
                    $"Index {i / 3}: ({dataArray[i]}, {dataArray[i + 1]}, {dataArray[i + 2]})"
                );
            }
        }

        /// Downloads tensor data to NativeArray.
        public static NativeArray<float> DownloadTensorData(Tensor<float> outputTensor)
        {
            if (outputTensor == null)
            {
                Debug.LogError("Output tensor is null.");
                return default;
            }

            // Debug.Log("Downloading tensor data to NativeArray");
            try
            {
                var nativeArray = outputTensor.DownloadToNativeArray();
                // Debug.Log("Successfully downloaded tensor data.");
                return nativeArray;
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"Error downloading tensor data: {e.Message}\nStackTrace: {e.StackTrace}"
                );
                return default;
            }
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
            try
            {
                // Debug.Log("Preparing output tensor...");

                // Dispose of previous tensor if exists
                if (currentOutputTensor != null)
                {
                    currentOutputTensor.Dispose();
                    currentOutputTensor = null;
                }

                // Ensure the new tensor is ready
                outputTensor.CompleteAllPendingOperations();

                // Create a new instance to avoid reusing the same memory
                currentOutputTensor = new Tensor<float>(outputTensor.shape);

                // Debug.Log($"Tensor prepared. Shape: {outputTensor.shape}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error preparing tensor: {e.Message}\nStackTrace: {e.StackTrace}");
                currentOutputTensor = null;
            }
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
