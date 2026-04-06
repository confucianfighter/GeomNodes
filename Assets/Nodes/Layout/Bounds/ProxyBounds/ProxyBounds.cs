using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DLN
{
    [ExecuteAlways]
    [RequireComponent(typeof(SmartBounds))]
    public class ProxyBounds : MonoBehaviour
    {
        [Header("Shared Visual Prefabs")]
        public Visual cornerPrefab;
        public StraightLineBetweenPoints linePrefab;

        [Header("Optional Bounds Source")]
        public GameObject target;

        [Header("Generated Parent")]
        public Transform visualsRoot;

        [Header("Visual Sets")]
        public BoundsBoxVisuals contentsVisuals;
        public BoundsBoxVisuals paddingVisuals;
        public BoundsBoxVisuals bordersVisuals;
        public BoundsBoxVisuals actualReportingVisuals;

        [Header("Display Toggles")]
        public bool showActualReporting = true;
        public bool showContents = true;
        public bool showBorders = true;
        public bool showPadding = true;

        [Header("Editor Behavior")]
        public bool showOnlyWhenSelected = true;

        [Header("Colors")]
        public Color contentsColor = Color.green;
        public Color paddingColor = Color.cyan;
        public Color bordersColor = Color.yellow;
        public Color actualReportingColor = Color.white;

        private const string VisualsRootName = "Visuals";
        private const string ContentsChildName = "Contents";
        private const string PaddingChildName = "Padding";
        private const string BordersChildName = "Borders";
        private const string ActualReportingChildName = "ActualReporting";

        private Transform _lastTargetTransform;
        private Vector3 _lastTargetPosition;
        private Quaternion _lastTargetRotation;
        private Vector3 _lastTargetScale;

        private void Reset()
        {
            if (target == null)
                target = transform.parent != null ? transform.parent.gameObject : gameObject;
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            Selection.selectionChanged += OnEditorSelectionChanged;
#endif
            RefreshAll();
            CacheTargetTransformState();
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            Selection.selectionChanged -= OnEditorSelectionChanged;
#endif
        }

        private void OnValidate()
        {
            if (target == null)
                target = transform.parent != null ? transform.parent.gameObject : gameObject;

            RefreshAll();
            CacheTargetTransformState();
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (TargetTransformChanged())
                {
                    RefreshAll();
                    CacheTargetTransformState();
                }
            }
#endif
        }

        private void OnTransformParentChanged()
        {
            RefreshAll();
            CacheTargetTransformState();
        }

        private void OnTransformChildrenChanged()
        {
            RefreshAll();
            CacheTargetTransformState();
        }

#if UNITY_EDITOR
        private void OnEditorSelectionChanged()
        {
            ApplyRootVisibility();
        }
#endif

        [ContextMenu("Generate / Ensure Setup")]
        public void Generate()
        {
            EnsureSetup();
            ApplyChildVisibility();
            ApplyRootVisibility();
        }

        [ContextMenu("Refresh All")]
        public void RefreshAll()
        {
            EnsureSetup();
            RefreshBounds();
            ApplyChildVisibility();
            ApplyRootVisibility();
        }

        [ContextMenu("Update Bounds")]
        public void UpdateProxyBounds()
        {
            RefreshAll();
        }

        [ContextMenu("Show All Types")]
        public void ShowAll()
        {
            showActualReporting = true;
            showContents = true;
            showBorders = true;
            showPadding = true;
            ApplyChildVisibility();
            ApplyRootVisibility();
        }

        [ContextMenu("Show Only Actual Reporting")]
        public void ShowActualReportingOnly()
        {
            showActualReporting = true;
            showContents = false;
            showBorders = false;
            showPadding = false;
            ApplyChildVisibility();
            ApplyRootVisibility();
        }

        [ContextMenu("Show Only Contents")]
        public void ShowContentsOnly()
        {
            showActualReporting = false;
            showContents = true;
            showBorders = false;
            showPadding = false;
            ApplyChildVisibility();
            ApplyRootVisibility();
        }

        [ContextMenu("Show Only Borders")]
        public void ShowBordersOnly()
        {
            showActualReporting = false;
            showContents = false;
            showBorders = true;
            showPadding = false;
            ApplyChildVisibility();
            ApplyRootVisibility();
        }

        [ContextMenu("Show Only Padding")]
        public void ShowPaddingOnly()
        {
            showActualReporting = false;
            showContents = false;
            showBorders = false;
            showPadding = true;
            ApplyChildVisibility();
            ApplyRootVisibility();
        }

        [ContextMenu("Clear Generated Visuals")]
        public void ClearGenerated()
        {
            if (contentsVisuals != null) DestroyVisualObject(contentsVisuals.gameObject);
            if (paddingVisuals != null) DestroyVisualObject(paddingVisuals.gameObject);
            if (bordersVisuals != null) DestroyVisualObject(bordersVisuals.gameObject);
            if (actualReportingVisuals != null) DestroyVisualObject(actualReportingVisuals.gameObject);

            contentsVisuals = null;
            paddingVisuals = null;
            bordersVisuals = null;
            actualReportingVisuals = null;

            if (visualsRoot != null)
                DestroyVisualObject(visualsRoot.gameObject);

            visualsRoot = null;
        }

        public Bounds? GetSelfBounds()
        {
            if (actualReportingVisuals == null)
                return null;

            return actualReportingVisuals.GetCurrentLocalBounds();
        }

        private void EnsureSetup()
        {
            if (target == null)
                target = transform.parent != null ? transform.parent.gameObject : gameObject;

            if (target == null)
            {
                Debug.LogError($"No target found for ProxyBounds on {name}", this);
                return;
            }

            if (cornerPrefab == null || linePrefab == null)
            {
                Debug.LogError($"Missing cornerPrefab or linePrefab on {name}", this);
                return;
            }

            visualsRoot = GetOrCreateChild(transform, VisualsRootName).transform;
            SmartBounds sb;
            if (!visualsRoot.gameObject.TryGetComponent<SmartBounds>(out sb))
            {
                sb = visualsRoot.gameObject.AddComponent<SmartBounds>();
            }
            sb.settings.includeSelf = false;
            sb.settings.includeSelf = false;
            contentsVisuals = GetOrCreateVisuals(ref contentsVisuals, ContentsChildName, "Contents", contentsColor);
            paddingVisuals = GetOrCreateVisuals(ref paddingVisuals, PaddingChildName, "Padding", paddingColor);
            bordersVisuals = GetOrCreateVisuals(ref bordersVisuals, BordersChildName, "Borders", bordersColor);
            actualReportingVisuals = GetOrCreateVisuals(ref actualReportingVisuals, ActualReportingChildName, "Actual Reporting", actualReportingColor);
        }

        private BoundsBoxVisuals GetOrCreateVisuals(ref BoundsBoxVisuals field, string childName, string label, Color color)
        {
            GameObject child = GetOrCreateChild(visualsRoot, childName);

            field = child.GetComponent<BoundsBoxVisuals>();
            if (field == null)
                field = child.AddComponent<BoundsBoxVisuals>();

            field.label = label;
            field.color = color;
            field.cornerPrefab = cornerPrefab;
            field.linePrefab = linePrefab;
            field.EnsureGenerated();

            return field;
        }

        private void RefreshBounds()
        {
            if (target == null)
                return;

            if (contentsVisuals != null)
            {
                OptionalBoundsSettings overrides = OptionalBoundsSettings.Empty;
                overrides.SetRegionSelection(RegionSelection.Contents);
                contentsVisuals.ApplyBounds(target.ToLocalBounds(overrides));
            }

            if (paddingVisuals != null)
            {
                OptionalBoundsSettings overrides = OptionalBoundsSettings.Empty;
                overrides.SetRegionSelection(RegionSelection.Padding);
                paddingVisuals.ApplyBounds(target.ToLocalBounds(overrides));
            }

            if (bordersVisuals != null)
            {
                OptionalBoundsSettings overrides = OptionalBoundsSettings.Empty;
                overrides.SetRegionSelection(RegionSelection.Borders);
                bordersVisuals.ApplyBounds(target.ToLocalBounds(overrides));
            }

            if (actualReportingVisuals != null)
            {
                actualReportingVisuals.ApplyBounds(target.ToLocalBounds());
            }
        }

        private void ApplyChildVisibility()
        {
            SetBoxActive(contentsVisuals, showContents);
            SetBoxActive(paddingVisuals, showPadding);
            SetBoxActive(bordersVisuals, showBorders);
            SetBoxActive(actualReportingVisuals, showActualReporting);
        }

        private void ApplyRootVisibility()
        {
            if (visualsRoot == null)
                return;

            bool shouldShowRoot = ShouldShowVisualsRoot();

            if (visualsRoot.gameObject.activeSelf != shouldShowRoot)
                visualsRoot.gameObject.SetActive(shouldShowRoot);
        }

        private bool ShouldShowVisualsRoot()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return true;

            if (!showOnlyWhenSelected)
                return true;

            GameObject selected = Selection.activeGameObject;
            if (selected == null)
                return false;

            return selected == gameObject || selected == target;
#else
            return true;
#endif
        }

        private bool TargetTransformChanged()
        {
            Transform t = target != null ? target.transform : null;
            if (t == null)
                return _lastTargetTransform != null;

            if (_lastTargetTransform != t)
                return true;

            return t.position != _lastTargetPosition
                || t.rotation != _lastTargetRotation
                || t.lossyScale != _lastTargetScale;
        }

        private void CacheTargetTransformState()
        {
            Transform t = target != null ? target.transform : null;
            _lastTargetTransform = t;

            if (t == null)
                return;

            _lastTargetPosition = t.position;
            _lastTargetRotation = t.rotation;
            _lastTargetScale = t.lossyScale;
        }

        private static void SetBoxActive(BoundsBoxVisuals visuals, bool active)
        {
            if (visuals == null)
                return;

            if (visuals.gameObject.activeSelf != active)
                visuals.gameObject.SetActive(active);
        }

        private static GameObject GetOrCreateChild(Transform parent, string childName)
        {
            Transform existing = parent.Find(childName);
            if (existing != null)
                return existing.gameObject;

            GameObject go = new GameObject(childName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go;
        }

        private static void DestroyVisualObject(GameObject go)
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