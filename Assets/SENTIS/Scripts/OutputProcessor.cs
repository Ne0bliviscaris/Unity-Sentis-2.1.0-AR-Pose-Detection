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

        /// Validates tensor and initializes keypoints array.
        private KeyPoint[] ValidateAndInitializeKeypoints(Tensor<float> outputTensor)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(OutputProcessor));

            var keypoints = new KeyPoint[NUM_KEYPOINTS];

            if (!OutputUtils.IsOutputTensorValid(outputTensor))
                return keypoints;

            return keypoints;
        }

        // OutputProcessor.cs
        public KeyPoint[] ProcessModelOutput(Tensor<float> outputTensor)
        {
            var keypoints = ValidateAndInitializeKeypoints(outputTensor);

            if (keypoints.Length == 0)
                return keypoints;

            try
            {
                OutputUtils.PrepareOutputTensor(ref currentOutputTensor, outputTensor);
                ProcessTensorData(outputTensor, keypoints);

                return keypoints;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error processing tensor: {e.Message}\nStackTrace: {e.StackTrace}");
                return keypoints;
            }
        }

        /// Processes tensor data and fills keypoints array.
        private void ProcessTensorData(Tensor<float> outputTensor, KeyPoint[] keypoints)
        {
            // Download tensor data to NativeArray
            NativeArray<float> dataArray = OutputUtils.DownloadTensorData(outputTensor);

            // Process keypoints from dataArray
            ProcessKeypointsFromArray(dataArray, keypoints);

            // Dispose of NativeArray after use
            dataArray.Dispose();
        }

        private void ProcessKeypointsFromArray(NativeArray<float> dataArray, KeyPoint[] keypoints)
        {
            try
            {
                if (!OutputUtils.ValidateDataArrayLength(dataArray, NUM_KEYPOINTS * 3))
                    return;

                OutputUtils.ClearKeypoints(keypoints, NUM_KEYPOINTS);
                OutputUtils.ExtractKeypoints(
                    dataArray,
                    keypoints,
                    NUM_KEYPOINTS,
                    confidenceThreshold
                );
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
