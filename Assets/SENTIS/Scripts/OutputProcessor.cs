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
            // Direct access to tensor data using indexer
            var tensorShape = outputTensor.shape;
            int numKeypoints = tensorShape[1]; // Shape is [1, 56, 8400]
            float modelWidth = 640f; // Typowe wymiary wejściowe modelu
            float modelHeight = 640f;
            for (int i = 0; i < numKeypoints && i < NUM_KEYPOINTS; i++)
            {
                // Get x, y, confidence for each keypoint
                float x = outputTensor[0, i, 0];
                float y = outputTensor[0, i, 1];
                float confidence = outputTensor[0, i, 2];

                // Normalize coordinates to 0-1 range
                x = Mathf.Clamp01(x / modelWidth);
                y = Mathf.Clamp01(y / modelHeight);

                // Filter by confidence threshold
                if (confidence >= confidenceThreshold)
                {
                    keypoints[i] = new KeyPoint(new Vector2(x, y), confidence);
                }
                else
                {
                    keypoints[i] = new KeyPoint(Vector2.zero, 0f);
                }
                ; // Log keypoint data
                string keypointName = ((KeypointName)i).ToString();
                // Debug.Log(
                //     $"Keypoint {i} ({keypointName}): Position(x:{x:F2}, y:{y:F2}), Confidence: {confidence:F3}"
                // );
            }
        }
    }
}
