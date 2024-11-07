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
        private float confidenceThreshold = 0.2f;

        [SerializeField]
        private float scaleFactor = 1f;

        [SerializeField]
        private float zOffset = -1;

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
            if (keypoints == null || keypointObjects == null)
                return;

            for (int i = 0; i < keypoints.Length; i++)
            {
                bool isVisible = keypoints[i].Confidence >= confidenceThreshold;
                keypointObjects[i].SetActive(isVisible);

                if (isVisible)
                {
                    Vector3 worldPosition = new Vector3(
                        keypoints[i].Position.x * scaleFactor,
                        keypoints[i].Position.y * scaleFactor,
                        zOffset // Z offset to ensure keypoint is in front of the camera
                    );

                    keypointObjects[i].transform.position = worldPosition;
                    // Ensure label faces camera
                    if (Camera.main != null)
                    {
                        keypointLabels[i].transform.rotation = Camera.main.transform.rotation;
                    }

                    Debug.Log(
                        $"Keypoint {(KeypointName)i}: Position={worldPosition}, Confidence={keypoints[i].Confidence:F3}"
                    );
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
