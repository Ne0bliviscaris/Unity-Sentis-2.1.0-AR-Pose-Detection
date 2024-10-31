using Unity.Sentis;
using UnityEngine;

// TensorConverter.cs - odpowiedzialny za konwersję do tensora
namespace Sentis
{
    public class TensorConverter
    {
        private readonly int imageSize;

        public TensorConverter(int imageSize)
        {
            this.imageSize = imageSize;
        }

        public Tensor<float> ImageToTensor(Texture2D image)
        {
            var shape = new TensorShape(1, 3, imageSize, imageSize);
            var tensor = new Tensor<float>(shape);

            var pixels = image.GetPixels32();
            var tensorData = new float[shape.length];

            ConvertPixelsToTensorData(pixels, tensorData);
            tensor.Upload(tensorData);

            return tensor;
        }

        private void ConvertPixelsToTensorData(Color32[] pixels, float[] tensorData)
        {
            for (int y = 0; y < imageSize; y++)
            {
                for (int x = 0; x < imageSize; x++)
                {
                    int pixelIdx = y * imageSize + x;
                    Color32 pixel = pixels[pixelIdx];

                    // CHW format
                    tensorData[GetTensorIndex(0, x, y)] = pixel.r / 255f;
                    tensorData[GetTensorIndex(1, x, y)] = pixel.g / 255f;
                    tensorData[GetTensorIndex(2, x, y)] = pixel.b / 255f;
                }
            }
        }

        private int GetTensorIndex(int channel, int x, int y)
        {
            return channel * imageSize * imageSize + y * imageSize + x;
        }
    }
}
