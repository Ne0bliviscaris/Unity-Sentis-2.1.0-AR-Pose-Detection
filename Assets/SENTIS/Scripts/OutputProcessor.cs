using System;
using Unity.Collections;
using Unity.Sentis;
using UnityEngine;

/// Processes model output tensor into keypoints
namespace Sentis
{
    public class OutputProcessor : IDisposable
    {
        private const int NUM_KEYPOINTS = 17;
        private readonly float confidenceThreshold;
        private bool disposed = false;
        private Tensor currentOutputTensor;

        public OutputProcessor(float confidenceThreshold = 0.5f)
        {
            this.confidenceThreshold = confidenceThreshold;
        }

        public void Dispose()
        {
            if (!disposed)
            {
                currentOutputTensor?.Dispose();
                disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        ~OutputProcessor()
        {
            Dispose();
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

        public KeyPoint[] ProcessOutput(Tensor<float> outputTensor)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(OutputProcessor));

            var keypoints = new KeyPoint[NUM_KEYPOINTS];

            if (outputTensor == null || outputTensor.shape.length == 0)
            {
                Debug.LogError("Output tensor is null or empty");
                return keypoints;
            }

            try
            {
                currentOutputTensor?.Dispose();
                currentOutputTensor = outputTensor;

                // Ensure all pending operations are completed
                outputTensor.CompleteAllPendingOperations();

                // Download tensor data to NativeArray
                NativeArray<float> dataArray = outputTensor.DownloadToNativeArray();

                // Process keypoints from dataArray
                ProcessKeypointsFromArray(dataArray, keypoints);

                // Dispose of NativeArray after use
                dataArray.Dispose();

                return keypoints;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error processing tensor: {e.Message}\nStackTrace: {e.StackTrace}");
                return keypoints;
            }
        }

        private void ProcessKeypointsFromArray(NativeArray<float> dataArray, KeyPoint[] keypoints)
        {
            try
            {
                // Check if data array has sufficient length
                if (dataArray.Length < NUM_KEYPOINTS * 3)
                {
                    Debug.LogError(
                        $"Invalid data array length. Expected: {NUM_KEYPOINTS * 3}, Got: {dataArray.Length}"
                    );
                    return;
                }

                // Clear previous keypoints
                for (int i = 0; i < NUM_KEYPOINTS; i++)
                {
                    keypoints[i] = new KeyPoint(Vector2.zero, 0f);
                }

                for (int i = 0; i < NUM_KEYPOINTS; i++)
                {
                    // Extract x, y, confidence from data array
                    float x = dataArray[i * 3];
                    float y = dataArray[i * 3 + 1];
                    float conf = dataArray[i * 3 + 2];

                    if (conf >= confidenceThreshold)
                    {
                        keypoints[i] = new KeyPoint(new Vector2(x, y), conf);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"Error processing keypoints: {e.Message}\nStackTrace: {e.StackTrace}"
                );
            }
        }
    }
}
