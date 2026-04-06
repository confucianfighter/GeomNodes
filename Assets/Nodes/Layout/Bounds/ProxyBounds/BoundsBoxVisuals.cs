using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DLN
{
    public class BoundsBoxVisuals : MonoBehaviour
    {
        [Header("Identity")]
        public string label = "Bounds";
        public Color color = Color.white;

        [Header("Shared Prefabs")]
        public Visual cornerPrefab;
        public StraightLineBetweenPoints linePrefab;

        [Header("Runtime / Anchor Visuals")]
        public bool showCornerAnchors = true;
        public bool showLineAnchors = true;

        [Header("Editor Overlay")]
        public bool drawEditorOverlay = true;
        public bool drawOnlyWhenSelected = true;
        [Min(1f)] public float editorLineThickness = 10f;
        public bool drawTutorialLabel = true;
        public Vector3 editorLabelLocalOffset = new Vector3(0f, 0.025f, 0f);
        [Min(1)] public int editorLabelFontSize = 11;

        [Header("Generated")]
        [SerializeField] private List<Visual> corners = new();
        [SerializeField] private List<StraightLineBetweenPoints> lines = new();

        [Header("Current Bounds")]
        [SerializeField] private bool hasCurrentBounds;
        [SerializeField] private Bounds currentLocalBounds;

        private static readonly string[] CornerNames =
        {
            "Corner_LBB",
            "Corner_RBB",
            "Corner_LTB",
            "Corner_RTB",
            "Corner_LBF",
            "Corner_RBF",
            "Corner_LTF",
            "Corner_RTF"
        };

        private static readonly int[,] EdgePairs =
        {
            {0,1}, {1,3}, {3,2}, {2,0}, // back face
            {4,5}, {5,7}, {7,6}, {6,4}, // front face
            {0,4}, {1,5}, {2,6}, {3,7}  // side connections
        };

        public IReadOnlyList<Visual> Corners => corners;
        public IReadOnlyList<StraightLineBetweenPoints> Lines => lines;
        public bool HasCurrentBounds => hasCurrentBounds;
        public Bounds CurrentLocalBounds => currentLocalBounds;

        public void EnsureGenerated()
        {
            PruneNulls();

            if (HasFullGeneratedSet())
            {
                EnsureLineBindings();
                ApplyColor();
                ApplyAnchorVisibility();
                return;
            }

            Generate();
        }

        [ContextMenu("Generate()")]
        public void Generate()
        {
            ClearGenerated();

            if (cornerPrefab == null || linePrefab == null)
            {
                Debug.LogError($"BoundsBoxVisuals '{name}' is missing cornerPrefab or linePrefab.", this);
                return;
            }

            for (int i = 0; i < 8; i++)
            {
                Visual corner = InstantiatePrefab(cornerPrefab, transform);
                corner.name = CornerNames[i];
                corner.transform.localPosition = Vector3.zero;
                corner.transform.localRotation = Quaternion.identity;
                corner.transform.localScale = Vector3.one;
                corners.Add(corner);
            }

            for (int i = 0; i < EdgePairs.GetLength(0); i++)
            {
                int a = EdgePairs[i, 0];
                int b = EdgePairs[i, 1];

                StraightLineBetweenPoints line = InstantiatePrefab(linePrefab, transform);
                line.name = $"Edge_{ShortCornerName(CornerNames[a])}_{ShortCornerName(CornerNames[b])}";
                line.pointA = GetControlPoint(corners[a]);
                line.pointB = GetControlPoint(corners[b]);
                line.useLocalSpace = false;
                line.Refresh();
                lines.Add(line);
            }

            ApplyColor();
            ApplyAnchorVisibility();
        }

        [ContextMenu("ClearGenerated()")]
        public void ClearGenerated()
        {
            if (lines != null)
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i] != null)
                        DestroyObject(lines[i].gameObject);
                }
            }

            if (corners != null)
            {
                for (int i = 0; i < corners.Count; i++)
                {
                    if (corners[i] != null)
                        DestroyObject(corners[i].gameObject);
                }
            }

            corners.Clear();
            lines.Clear();
        }

        public void ApplyBounds(Bounds? bounds)
        {
            EnsureGenerated();

            if (!bounds.HasValue)
            {
                hasCurrentBounds = false;
                SetVisualEnabled(false);
                return;
            }

            hasCurrentBounds = true;
            currentLocalBounds = bounds.Value;

            if (!HasFullGeneratedSet())
            {
                Debug.LogWarning($"BoundsBoxVisuals '{name}' is missing generated visuals.", this);
                return;
            }

            Vector3[] positions = GetBoxCornerPositions(bounds.Value);

            for (int i = 0; i < 8; i++)
            {
                if (corners[i] == null)
                    continue;

                corners[i].transform.localPosition = positions[i];
                corners[i].transform.localRotation = Quaternion.identity;
                corners[i].transform.localScale = Vector3.one;
            }

            RefreshLines();
            ApplyColor();
            ApplyAnchorVisibility();
        }

        public void ClearBounds()
        {
            hasCurrentBounds = false;
            SetVisualEnabled(false);
        }

        public void SetVisualEnabled(bool enabled)
        {
            if (!enabled)
            {
                SetCornersEnabled(false);
                SetLinesEnabled(false);
                return;
            }

            ApplyAnchorVisibility();
        }

        public void SetCornersEnabled(bool enabled)
        {
            if (corners == null)
                return;

            for (int i = 0; i < corners.Count; i++)
            {
                if (corners[i] != null)
                    corners[i].SetVisualEnabled(enabled);
            }
        }

        public void SetLinesEnabled(bool enabled)
        {
            if (lines == null)
                return;

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i] == null)
                    continue;

                LineRenderer lr = lines[i].GetComponent<LineRenderer>();
                if (lr != null)
                    lr.enabled = enabled;
            }
        }

        public void ApplyAnchorVisibility()
        {
            if (!hasCurrentBounds)
            {
                SetCornersEnabled(false);
                SetLinesEnabled(false);
                return;
            }

            SetCornersEnabled(showCornerAnchors);
            SetLinesEnabled(showLineAnchors);
        }

        public void RefreshLines()
        {
            if (lines == null)
                return;

            EnsureLineBindings();

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i] != null)
                    lines[i].Refresh();
            }
        }

        public void ApplyColor()
        {
            if (lines == null)
                return;

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i] == null)
                    continue;

                LineRenderer lr = lines[i].GetComponent<LineRenderer>();
                if (lr != null)
                {
                    lr.startColor = color;
                    lr.endColor = color;
                }
            }
        }

        public Bounds? GetCurrentLocalBounds()
        {
            return hasCurrentBounds ? currentLocalBounds : null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            
            DrawEditorOverlayNow();
        }

        private void OnDrawGizmosSelected()
        {
            
            DrawEditorOverlayNow();
        }

        private void DrawEditorOverlayNow()
        {
            if (!hasCurrentBounds)
                return;

            Vector3[] p = GetBoxCornerPositions(currentLocalBounds);

            using (new Handles.DrawingScope(color, transform.localToWorldMatrix))
            {
                for (int i = 0; i < EdgePairs.GetLength(0); i++)
                {
                    int a = EdgePairs[i, 0];
                    int b = EdgePairs[i, 1];
                    Handles.DrawAAPolyLine(editorLineThickness, new Vector3[] { p[a], p[b] });
                }
            }

            if (drawTutorialLabel)
                DrawTutorialLabelTopFrontLeft(p[6], label);
        }

        private void DrawTutorialLabelTopFrontLeft(Vector3 localTopFrontLeft, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            Vector3 worldPos = transform.TransformPoint(localTopFrontLeft + editorLabelLocalOffset);

            GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = color;
            style.fontSize = editorLabelFontSize;
            style.alignment = TextAnchor.LowerLeft;

            Handles.Label(worldPos, text, style);
        }
#endif

        private bool HasFullGeneratedSet()
        {
            return corners != null && lines != null && corners.Count >= 8 && lines.Count >= 12;
        }

        private void EnsureLineBindings()
        {
            if (corners == null || lines == null || corners.Count < 8 || lines.Count < 12)
                return;

            for (int i = 0; i < EdgePairs.GetLength(0) && i < lines.Count; i++)
            {
                if (lines[i] == null)
                    continue;

                int a = EdgePairs[i, 0];
                int b = EdgePairs[i, 1];

                lines[i].pointA = GetControlPoint(corners[a]);
                lines[i].pointB = GetControlPoint(corners[b]);
                lines[i].useLocalSpace = false;
            }
        }

        private void PruneNulls()
        {
            if (corners != null)
                corners.RemoveAll(x => x == null);

            if (lines != null)
                lines.RemoveAll(x => x == null);
        }

        private Transform GetControlPoint(Visual corner)
        {
            if (corner == null)
                return null;

            if (corner.visual != null)
                return corner.visual.transform;

            return corner.transform;
        }

        private Vector3[] GetBoxCornerPositions(Bounds bounds)
        {
            Vector3 c = bounds.center;
            Vector3 e = bounds.extents;

            return new Vector3[]
            {
                c + new Vector3(-e.x, -e.y, -e.z), // 0 LBB
                c + new Vector3( e.x, -e.y, -e.z), // 1 RBB
                c + new Vector3(-e.x,  e.y, -e.z), // 2 LTB
                c + new Vector3( e.x,  e.y, -e.z), // 3 RTB
                c + new Vector3(-e.x, -e.y,  e.z), // 4 LBF
                c + new Vector3( e.x, -e.y,  e.z), // 5 RBF
                c + new Vector3(-e.x,  e.y,  e.z), // 6 LTF
                c + new Vector3( e.x,  e.y,  e.z)  // 7 RTF
            };
        }

        private string ShortCornerName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                return "Unknown";

            const string prefix = "Corner_";
            if (fullName.StartsWith(prefix))
                return fullName.Substring(prefix.Length);

            return fullName;
        }

        private static T InstantiatePrefab<T>(T prefab, Transform parent) where T : Component
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object obj = PrefabUtility.InstantiatePrefab(prefab.gameObject, parent);
                return ((GameObject)obj).GetComponent<T>();
            }
#endif
            return Object.Instantiate(prefab, parent);
        }

        private static void DestroyObject(GameObject go)
        {
            if (go == null)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(go);
                return;
            }
#endif
            Object.Destroy(go);
        }
    }
}