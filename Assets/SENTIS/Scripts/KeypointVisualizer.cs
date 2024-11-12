// KeypointVisualizer.cs
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace Sentis
{
    /// Handles drawing keypoints on camera preview
    public class KeypointVisualizer : MonoBehaviour
    {
        [SerializeField]
        private GameObject keypointPrefab;

        [SerializeField]
        private float confidenceThreshold = 0.5f;

        [SerializeField]
        private Camera _targetCamera;
        public Camera TargetCamera
        {
            get => _targetCamera;
            set => _targetCamera = value;
        }

        [SerializeField]
        private UnityEngine.UI.RawImage _cameraPreview;
        public UnityEngine.UI.RawImage CameraPreview
        {
            get => _cameraPreview;
            set => _cameraPreview = value;
        }

        [SerializeField]
        private float labelOffset = 0.1f; // Offset dla etykiety nad punktem

        private GameObject[] keypointObjects;
        private TextMesh[] keypointLabels;
        private const int NUM_KEYPOINTS = 17;

        private void Start()
        {
            InitializeKeypointObjects();
        }

        private void InitializeKeypointObjects()
        {
            keypointObjects = new GameObject[NUM_KEYPOINTS];
            keypointLabels = new TextMesh[NUM_KEYPOINTS];

#if UNITY_EDITOR
            // Create sorting layer if it doesn't exist
            if (!SortingLayer.layers.ToList().Any(l => l.name == "Keypoints"))
            {
                SerializedObject tagManager = new SerializedObject(
                    AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]
                );
                SerializedProperty layers = tagManager.FindProperty("m_SortingLayers");
                layers.InsertArrayElementAtIndex(layers.arraySize);
                var newLayer = layers.GetArrayElementAtIndex(layers.arraySize - 1);
                newLayer.FindPropertyRelative("name").stringValue = "Keypoints";
                newLayer.FindPropertyRelative("uniqueID").intValue =
                    Mathf.Max(SortingLayer.layers.Select(l => l.id).Max(), 0) + 1;
                tagManager.ApplyModifiedProperties();
            }
#endif

            for (int i = 0; i < NUM_KEYPOINTS; i++)
            {
                // Create keypoint object
                keypointObjects[i] = Instantiate(keypointPrefab, Vector3.zero, Quaternion.identity);
                keypointObjects[i].name = $"Keypoint_{((KeypointName)i)}";
                keypointObjects[i].transform.SetParent(transform);

                // Set rendering properties
                var renderer = keypointObjects[i].GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sortingLayerName = "Keypoints";
                    renderer.sortingOrder = 1;

                    // Make sure material renders over everything
                    var material = renderer.material;
                    material.renderQueue = 4000; // Render after transparent objects
                }

                // Setup label with same rendering properties
                var labelObj = CreateLabel(i);
                keypointLabels[i] = labelObj.GetComponent<TextMesh>();
                var textRenderer = labelObj.GetComponent<MeshRenderer>();
                if (textRenderer != null)
                {
                    textRenderer.sortingLayerName = "Keypoints";
                    textRenderer.sortingOrder = 2;
                    textRenderer.material.renderQueue = 4001;
                }

                keypointObjects[i].SetActive(false);
            }
        }

        public void UpdateKeypoints(KeyPoint[] keypoints)
        {
            if (
                keypoints == null
                || keypointObjects == null
                || CameraPreview == null
                || TargetCamera == null
            )
                return;

            // Get preview dimensions from RawImage
            var rect = CameraPreview.rectTransform.rect;
            Vector2 screenDimensions = new Vector2(rect.width, rect.height);

            for (int i = 0; i < keypoints.Length; i++)
            {
                bool isVisible = keypoints[i].Confidence >= confidenceThreshold;
                keypointObjects[i].SetActive(isVisible);

                if (isVisible)
                {
                    // Convert normalized coordinates to screen space
                    Vector2 screenPosition = new Vector2(
                        keypoints[i].Position.x * screenDimensions.x,
                        (1 - keypoints[i].Position.y) * screenDimensions.y // Maintain Y inversion
                    );

                    // Check if screen position is valid
                    if (float.IsNaN(screenPosition.x) || float.IsNaN(screenPosition.y))
                    {
                        Debug.LogWarning(
                            $"Invalid screen position (screen pos {screenPosition.x}, {screenPosition.y})"
                        );
                        continue;
                    }

                    // Check if screen position is within the view frustum
                    if (
                        screenPosition.x < 0
                        || screenPosition.x > screenDimensions.x
                        || screenPosition.y < 0
                        || screenPosition.y > screenDimensions.y
                    )
                    {
                        Debug.LogWarning(
                            $"Screen position out of view frustum (screen pos {screenPosition.x}, {screenPosition.y}) (Camera rect {rect.x} {rect.y} {rect.width} {rect.height})"
                        );
                        continue;
                    }

                    // Convert to world space using target camera
                    Vector3 worldPosition = TargetCamera.ScreenToWorldPoint(
                        new Vector3(
                            screenPosition.x,
                            screenPosition.y,
                            TargetCamera.nearClipPlane + 1.0f
                        )
                    );

                    keypointObjects[i].transform.position = worldPosition;

                    // Set a consistent scale for keypoint objects
                    keypointObjects[i].transform.localScale = Vector3.one * 600.0f; // Adjust scale as needed
                    // Update label rotation
                    if (keypointLabels[i] != null)
                    {
                        keypointLabels[i].transform.rotation = TargetCamera.transform.rotation;
                    }
                }
            }
        }

        private GameObject CreateLabel(int index)
        {
            var labelObj = new GameObject($"Label_{((KeypointName)index)}");
            labelObj.transform.SetParent(keypointObjects[index].transform);

            var textMesh = labelObj.AddComponent<TextMesh>();
            textMesh.text = ((KeypointName)index).ToString();
            textMesh.fontSize = 14;
            textMesh.anchor = TextAnchor.LowerCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = 0.01f;

            labelObj.transform.localPosition = Vector3.up * labelOffset;

            return labelObj;
        }

        private void OnDestroy()
        {
            if (keypointObjects != null)
            {
                foreach (var obj in keypointObjects)
                {
                    if (obj != null)
                        Destroy(obj);
                }
            }
        }
    }
}
