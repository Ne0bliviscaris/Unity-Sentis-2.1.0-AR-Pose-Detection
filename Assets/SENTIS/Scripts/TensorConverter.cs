using System;
using Unity.Sentis;
using UnityEngine;

namespace Sentis
{
    /// <summary>
    /// Handles conversion of images to Sentis tensors
    /// </summary>
    public class TensorConverter : IDisposable
    {
        private readonly int imageSize;
        private bool disposed = false;

        public TensorConverter(int imageSize)
        {
            this.imageSize = imageSize;
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Converts Unity Texture2D to Sentis tensor in CHW format
        /// </summary>
        public Tensor<float> ImageToTensor(Texture2D image)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(TensorConverter));

            if (image == null)
                throw new ArgumentNullException(nameof(image));

            var shape = new TensorShape(1, 3, imageSize, imageSize);
            var pixels = image.GetPixels32();
            var tensorData = new float[shape.length];

            ConvertPixelsToTensorData(pixels, tensorData);

            return new Tensor<float>(shape, tensorData);
        }

        /// <summary>
        /// Converts pixel array to normalized tensor data in CHW format
        /// </summary>
        private void ConvertPixelsToTensorData(Color32[] pixels, float[] tensorData)
        {
            int pixelsLength = pixels.Length;
            if (pixelsLength != imageSize * imageSize)
            {
                throw new ArgumentException(
                    $"Invalid pixel array length. Expected: {imageSize * imageSize}, Got: {pixelsLength}"
                );
            }

            for (int y = 0; y < imageSize; y++)
            {
                for (int x = 0; x < imageSize; x++)
                {
                    int pixelIdx = y * imageSize + x;
                    Color32 pixel = pixels[pixelIdx];

                    // CHW format (Channel, Height, Width)
                    tensorData[GetTensorIndex(0, x, y)] = pixel.r / 255f;
                    tensorData[GetTensorIndex(1, x, y)] = pixel.g / 255f;
                    tensorData[GetTensorIndex(2, x, y)] = pixel.b / 255f;
                }
            }
        }

        /// <summary>
        /// Calculates 1D index for tensor data in CHW format
        /// </summary>
        private int GetTensorIndex(int channel, int x, int y)
        {
            return channel * imageSize * imageSize + y * imageSize + x;
        }
    }
}
