using UnityEngine;

// ICameraProvider.cs
namespace Sentis
{
    /// <summary>
    /// Interface for camera input handling
    /// </summary>
    public interface ICameraProvider
    {
        Texture2D GetCurrentFrame();
        bool IsInitialized { get; }
        void Initialize();
        void StartCapture();
        void StopCapture();
    }
}
