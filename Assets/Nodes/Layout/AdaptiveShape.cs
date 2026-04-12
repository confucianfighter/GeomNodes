// using UnityEngine;

// #if UNITY_EDITOR
// using DLN.EditorTools.ShapeStamper;
// #endif

// namespace DLN
// {
//     [DisallowMultipleComponent]
//     public class AdaptiveShape : MonoBehaviour
//     {
//         [SerializeField] private SmartBounds smartBounds;
//         [SerializeField] private bool preferSmartBoundsBordersPadding = true;
//         [SerializeField] private BordersPadding fallbackBordersPadding = BordersPadding.Default;

//         [SerializeField] private Transform mainShapeRoot;
//         [SerializeField] private Transform ringSegmentsRoot;
//         [SerializeField] private Transform startCapRoot;
//         [SerializeField] private Transform endCapRoot;
//         [SerializeField] private Transform debugRoot;

// #if UNITY_EDITOR
//         [SerializeField] private ShapeCanvasDocument shapeDocument = new();
//         [SerializeField] private ProfileCanvasDocument profileDocument = new();
// #endif

//         public SmartBounds SmartBounds => smartBounds;
//         public Transform MainShapeRoot => mainShapeRoot;
//         public Transform RingSegmentsRoot => ringSegmentsRoot;
//         public Transform StartCapRoot => startCapRoot;
//         public Transform EndCapRoot => endCapRoot;
//         public Transform DebugRoot => debugRoot;

//         public bool PreferSmartBoundsBordersPadding
//         {
//             get => preferSmartBoundsBordersPadding;
//             set => preferSmartBoundsBordersPadding = value;
//         }

//         public BordersPadding FallbackBordersPadding
//         {
//             get => fallbackBordersPadding;
//             set
//             {
//                 fallbackBordersPadding = value;
//                 fallbackBordersPadding.ClampToValid();
//             }
//         }

// #if UNITY_EDITOR
//         public ShapeCanvasDocument ShapeDocument
//         {
//             get
//             {
//                 EnsureEditorState();
//                 return shapeDocument;
//             }
//         }

//         public ProfileCanvasDocument ProfileDocument
//         {
//             get
//             {
//                 EnsureEditorState();
//                 return profileDocument;
//             }
//         }
// #endif

//         private void Reset()
//         {
//             EnsureReferences();
//             fallbackBordersPadding.ClampToValid();
// #if UNITY_EDITOR
//             EnsureEditorState();
// #endif
//         }

//         private void OnValidate()
//         {
//             EnsureReferences();
//             fallbackBordersPadding.ClampToValid();
// #if UNITY_EDITOR
//             EnsureEditorState();
// #endif
//         }

//         public void EnsureReferences()
//         {
//             if (smartBounds == null)
//                 TryGetComponent(out smartBounds);
//         }

//         public void EnsureGeneratedHierarchy()
//         {
//             mainShapeRoot = EnsureChild(transform, mainShapeRoot, "MainShape");
//             ringSegmentsRoot = EnsureChild(mainShapeRoot, ringSegmentsRoot, "RingSegments");
//             startCapRoot = EnsureChild(mainShapeRoot, startCapRoot, "StartCap");
//             endCapRoot = EnsureChild(mainShapeRoot, endCapRoot, "EndCap");
//             debugRoot = EnsureChild(transform, debugRoot, "Debug");
//         }

//         private static Transform EnsureChild(Transform parent, Transform current, string childName)
//         {
//             if (parent == null)
//                 return current;

//             if (current != null && current.parent == parent)
//             {
//                 current.name = childName;
//                 return current;
//             }

//             Transform existing = parent.Find(childName);
//             if (existing != null)
//                 return existing;

//             GameObject go = new GameObject(childName);
//             go.transform.SetParent(parent, false);
//             return go.transform;
//         }

//         public BordersPadding GetEffectiveBordersPadding()
//         {
//             EnsureReferences();

//             BordersPadding result;
//             if (preferSmartBoundsBordersPadding && smartBounds != null)
//                 result = smartBounds.bordersPadding;
//             else
//                 result = fallbackBordersPadding;

//             result.ClampToValid();
//             return result;
//         }

//         public void PullFromSmartBounds()
//         {
//             EnsureReferences();
//             if (smartBounds == null)
//                 return;

//             fallbackBordersPadding = smartBounds.bordersPadding;
//             fallbackBordersPadding.ClampToValid();
//         }

//         public void PushFallbackToSmartBounds()
//         {
//             EnsureReferences();
//             if (smartBounds == null)
//                 return;

//             BordersPadding value = fallbackBordersPadding;
//             value.ClampToValid();
//             smartBounds.bordersPadding = value;
//         }

// #if UNITY_EDITOR
//         public void EnsureEditorState()
//         {
//             if (shapeDocument == null)
//                 shapeDocument = new ShapeCanvasDocument();

//             if (profileDocument == null)
//                 profileDocument = new ProfileCanvasDocument();

//             shapeDocument.EnsureValidShape();
//             profileDocument.EnsureValidProfile();
//         }
// #endif
//     }
// }
