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
        // /// Checks if the output tensor is valid.
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
