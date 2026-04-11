from pathlib import Path
import re
import sys

ROOT = Path.cwd()

ADAPTIVE_SHAPE = ROOT / "Assets/Nodes/Layout/AdaptiveShape.cs"
WINDOW = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/ShapeStamperWindow.cs"

def read(path: Path) -> str:
    if not path.exists():
        raise FileNotFoundError(f"Missing file: {path}")
    return path.read_text(encoding="utf-8")

def write(path: Path, text: str) -> None:
    path.write_text(text, encoding="utf-8", newline="\n")

def replace_or_fail(text: str, old: str, new: str, where: str) -> str:
    if old not in text:
        raise RuntimeError(f"Could not find expected block in {where}:\n{old[:160]}...")
    return text.replace(old, new, 1)

def sub_or_fail(pattern: str, repl: str, text: str, where: str, flags=re.DOTALL) -> str:
    new_text, count = re.subn(pattern, repl, text, count=1, flags=flags)
    if count != 1:
        raise RuntimeError(f"Could not replace pattern in {where}:\n{pattern}")
    return new_text

def write_adaptive_shape() -> None:
    code = """using UnityEngine;

#if UNITY_EDITOR
using DLN.EditorTools.ShapeStamper;
#endif

namespace DLN
{
    [DisallowMultipleComponent]
    public class AdaptiveShape : MonoBehaviour
    {
        [SerializeField] private SmartBounds smartBounds;
        [SerializeField] private bool preferSmartBoundsBordersPadding = true;
        [SerializeField] private BordersPadding fallbackBordersPadding = BordersPadding.Default;

#if UNITY_EDITOR
        [SerializeField] private ShapeCanvasDocument shapeDocument = new();
        [SerializeField] private ProfileCanvasDocument profileDocument = new();
#endif

        public SmartBounds SmartBounds => smartBounds;
        public bool PreferSmartBoundsBordersPadding
        {
            get => preferSmartBoundsBordersPadding;
            set => preferSmartBoundsBordersPadding = value;
        }

        public BordersPadding FallbackBordersPadding
        {
            get => fallbackBordersPadding;
            set
            {
                fallbackBordersPadding = value;
                fallbackBordersPadding.ClampToValid();
            }
        }

#if UNITY_EDITOR
        public ShapeCanvasDocument ShapeDocument
        {
            get
            {
                EnsureEditorState();
                return shapeDocument;
            }
        }

        public ProfileCanvasDocument ProfileDocument
        {
            get
            {
                EnsureEditorState();
                return profileDocument;
            }
        }
#endif

        private void Reset()
        {
            EnsureReferences();
            fallbackBordersPadding.ClampToValid();
#if UNITY_EDITOR
            EnsureEditorState();
#endif
        }

        private void OnValidate()
        {
            EnsureReferences();
            fallbackBordersPadding.ClampToValid();
#if UNITY_EDITOR
            EnsureEditorState();
#endif
        }

        public void EnsureReferences()
        {
            if (smartBounds == null)
                TryGetComponent(out smartBounds);
        }

        public BordersPadding GetEffectiveBordersPadding()
        {
            EnsureReferences();

            BordersPadding result;
            if (preferSmartBoundsBordersPadding && smartBounds != null)
                result = smartBounds.bordersPadding;
            else
                result = fallbackBordersPadding;

            result.ClampToValid();
            return result;
        }

        public void PullFromSmartBounds()
        {
            EnsureReferences();
            if (smartBounds == null)
                return;

            fallbackBordersPadding = smartBounds.bordersPadding;
            fallbackBordersPadding.ClampToValid();
        }

        public void PushFallbackToSmartBounds()
        {
            EnsureReferences();
            if (smartBounds == null)
                return;

            BordersPadding value = fallbackBordersPadding;
            value.ClampToValid();
            smartBounds.bordersPadding = value;
        }

#if UNITY_EDITOR
        public void EnsureEditorState()
        {
            if (shapeDocument == null)
                shapeDocument = new ShapeCanvasDocument();

            if (profileDocument == null)
                profileDocument = new ProfileCanvasDocument();

            shapeDocument.EnsureValidShape();
            profileDocument.EnsureValidProfile();
        }
#endif
    }
}
"""
    write(ADAPTIVE_SHAPE, code)
    print("Wrote AdaptiveShape.cs")

def patch_window() -> None:
    text = read(WINDOW)

    if "using DLN;" not in text:
        text = text.replace(
            "using System.Collections.Generic;\nusing UnityEditor;\nusing UnityEngine;\n",
            "using System.Collections.Generic;\nusing UnityEditor;\nusing UnityEngine;\nusing DLN;\n",
            1,
        )

    # Field swap
    text = replace_or_fail(
        text,
        '[SerializeField] private GameObject targetObject;\n',
        '[SerializeField] private AdaptiveShape adaptiveShape;\n',
        "ShapeStamperWindow.cs",
    )

    text = replace_or_fail(
        text,
        '        private bool _isDraggingDivider;\n        private DLN.SmartBounds _targetSmartBounds;\n',
        '        private bool _isDraggingDivider;\n        private SmartBounds _activeSmartBounds;\n',
        "ShapeStamperWindow.cs",
    )

    # OnEnable sync block
    text = replace_or_fail(
        text,
        """            DLN.SmartBounds smartBounds = EnsureTargetSmartBounds();
            if (smartBounds != null)
                SyncDocumentsFromSmartBounds(smartBounds);

            _shapePolicy ??= new ShapeCanvasPolicy(shapeDocument);
            _profilePolicy ??= new ProfileCanvasPolicy(profileDocument);

            _shapeCanvas = new EditorCanvas(shapeDocument, _shapePolicy, shapeSelection, shapeInteraction, shapeView);
            _profileCanvas = new EditorCanvas(profileDocument, _profilePolicy, profileSelection, profileInteraction, profileView);
""",
        """            BindToAdaptiveShape(adaptiveShape);

            _forcePreviewRefresh = true;
        }

        private void RecreateCanvasBindings()
        {
            _shapePolicy = new ShapeCanvasPolicy(shapeDocument);
            _profilePolicy = new ProfileCanvasPolicy(profileDocument);

            _shapeCanvas = new EditorCanvas(shapeDocument, _shapePolicy, shapeSelection, shapeInteraction, shapeView);
            _profileCanvas = new EditorCanvas(profileDocument, _profilePolicy, profileSelection, profileInteraction, profileView);
""",
        "ShapeStamperWindow.cs",
    )

    # remove old smartbounds sync in OnGUI
    text = replace_or_fail(
        text,
        """            DLN.SmartBounds smartBounds = EnsureTargetSmartBounds();
            if (smartBounds != null)
                SyncDocumentsFromSmartBounds(smartBounds);

            DrawTopBar();
""",
        """            SyncDocumentsFromAdaptiveShape();

            DrawTopBar();
""",
        "ShapeStamperWindow.cs",
    )

    # top bar header block swap
    text = replace_or_fail(
        text,
        """            EditorGUILayout.BeginHorizontal();
            GameObject newTargetObject = (GameObject)EditorGUILayout.ObjectField(
                "Target",
                targetObject,
                typeof(GameObject),
                true,
                GUILayout.Width(360f));

            if (newTargetObject != targetObject)
            {
                targetObject = newTargetObject;
                _targetSmartBounds = null;
                _forcePreviewRefresh = true;
            }

            DLN.SmartBounds smartBounds = EnsureTargetSmartBounds();

            if (smartBounds != null)
                EditorGUILayout.LabelField($"SmartBounds: {smartBounds.name}", EditorStyles.miniLabel);
            else
                EditorGUILayout.LabelField("Assign a target to drive real borders/padding.", EditorStyles.miniLabel);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
""",
        """            DrawAdaptiveShapeBindingRow();
""",
        "ShapeStamperWindow.cs",
    )

    # remove old direct inputs call
    text = text.replace("            DrawBordersPaddingInputs();\n", "            DrawAdaptiveShapeSummary();\n")

    # replace old methods block
    text = sub_or_fail(
        r"""        private void DrawBordersPaddingInputs\(\)\n        \{\n.*?\n        \}\n\n        private void DrawSelectedShapeElementInspector\(\)""",
        """        private void DrawAdaptiveShapeBindingRow()
        {
            EditorGUILayout.BeginHorizontal();

            AdaptiveShape selectedAdaptiveShape = TryGetSelectedAdaptiveShape();

            EditorGUILayout.LabelField(
                adaptiveShape != null
                    ? $"Adaptive Shape: {adaptiveShape.name}"
                    : "Adaptive Shape: none",
                EditorStyles.miniBoldLabel);

            if (GUILayout.Button("Use Selected", GUILayout.Width(100f)))
            {
                if (selectedAdaptiveShape != null)
                    BindToAdaptiveShape(selectedAdaptiveShape);
            }

            if (GUILayout.Button("Create Adaptive Shape", GUILayout.Width(160f)))
            {
                CreateAdaptiveShapeGameObject();
            }

            EditorGUI.BeginDisabledGroup(adaptiveShape == null);
            if (GUILayout.Button("Ping", GUILayout.Width(60f)))
            {
                EditorGUIUtility.PingObject(adaptiveShape);
                Selection.activeObject = adaptiveShape != null ? adaptiveShape.gameObject : null;
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAdaptiveShapeSummary()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Adaptive Shape", EditorStyles.boldLabel);

            if (adaptiveShape == null)
            {
                EditorGUILayout.HelpBox(
                    "Use Selected to bind the window to the currently selected AdaptiveShape, or Create Adaptive Shape to make a new one.",
                    MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            SmartBounds smartBounds = adaptiveShape.SmartBounds;
            BordersPadding bp = adaptiveShape.GetEffectiveBordersPadding();

            EditorGUILayout.LabelField(
                smartBounds != null
                    ? $"SmartBounds Source: {smartBounds.name}"
                    : "SmartBounds Source: none",
                EditorStyles.miniLabel);

            EditorGUILayout.LabelField(
                $"Using SmartBounds Borders/Padding: {adaptiveShape.PreferSmartBoundsBordersPadding}",
                EditorStyles.miniLabel);

            EditorGUILayout.LabelField(
                $"X  -B {bp.x.negativeBorder:0.###}   -P {bp.x.negativePadding:0.###}   +P {bp.x.positivePadding:0.###}   +B {bp.x.positiveBorder:0.###}",
                EditorStyles.miniLabel);

            EditorGUILayout.LabelField(
                $"Y  -B {bp.y.negativeBorder:0.###}   -P {bp.y.negativePadding:0.###}   +P {bp.y.positivePadding:0.###}   +B {bp.y.positiveBorder:0.###}",
                EditorStyles.miniLabel);

            EditorGUILayout.LabelField(
                $"Z  -B {bp.z.negativeBorder:0.###}   -P {bp.z.negativePadding:0.###}   +P {bp.z.positivePadding:0.###}   +B {bp.z.positiveBorder:0.###}",
                EditorStyles.miniLabel);

            EditorGUILayout.HelpBox(
                "Edit borders/padding on SmartBounds for now. The Shape Stamper window reads them and updates its guides/documents.",
                MessageType.None);

            EditorGUILayout.EndVertical();
        }

        private AdaptiveShape TryGetSelectedAdaptiveShape()
        {
            GameObject selectedGo = Selection.activeGameObject;
            if (selectedGo == null)
                return null;

            selectedGo.TryGetComponent(out AdaptiveShape selectedAdaptiveShape);
            return selectedAdaptiveShape;
        }

        private void CreateAdaptiveShapeGameObject()
        {
            GameObject go = new GameObject("Adaptive Shape");
            Undo.RegisterCreatedObjectUndo(go, "Create Adaptive Shape");

            SmartBounds smartBounds = Undo.AddComponent<SmartBounds>(go);
            AdaptiveShape newAdaptiveShape = Undo.AddComponent<AdaptiveShape>(go);

            newAdaptiveShape.EnsureReferences();
            newAdaptiveShape.PullFromSmartBounds();
#if UNITY_EDITOR
            newAdaptiveShape.EnsureEditorState();
#endif

            Selection.activeGameObject = go;
            BindToAdaptiveShape(newAdaptiveShape);

            EditorGUIUtility.PingObject(go);
        }

        private void BindToAdaptiveShape(AdaptiveShape newAdaptiveShape)
        {
            adaptiveShape = newAdaptiveShape;
            _activeSmartBounds = null;

            if (adaptiveShape == null)
            {
                shapeDocument ??= new ShapeCanvasDocument();
                profileDocument ??= new ProfileCanvasDocument();

                shapeDocument.EnsureValidShape();
                profileDocument.EnsureValidProfile();

                RecreateCanvasBindings();
                return;
            }

            adaptiveShape.EnsureReferences();
#if UNITY_EDITOR
            adaptiveShape.EnsureEditorState();
            shapeDocument = adaptiveShape.ShapeDocument;
            profileDocument = adaptiveShape.ProfileDocument;
#endif
            _activeSmartBounds = adaptiveShape.SmartBounds;

            SyncDocumentsFromAdaptiveShape();
            RecreateCanvasBindings();

            shapeSelection.Clear();
            shapeInteraction.Clear();
            profileSelection.Clear();
            profileInteraction.Clear();

            shapeView.ResetView();
            profileView.ResetView();

            _forcePreviewRefresh = true;
        }

        private void SyncDocumentsFromAdaptiveShape()
        {
            if (adaptiveShape == null)
                return;

            BordersPadding bp = adaptiveShape.GetEffectiveBordersPadding();
            bp.ClampToValid();

            bool shapeChanged =
                !Mathf.Approximately(shapeDocument.LeftBorder, bp.x.negativeBorder) ||
                !Mathf.Approximately(shapeDocument.LeftPadding, bp.x.negativePadding) ||
                !Mathf.Approximately(shapeDocument.RightPadding, bp.x.positivePadding) ||
                !Mathf.Approximately(shapeDocument.RightBorder, bp.x.positiveBorder) ||
                !Mathf.Approximately(shapeDocument.TopBorder, bp.y.negativeBorder) ||
                !Mathf.Approximately(shapeDocument.TopPadding, bp.y.negativePadding) ||
                !Mathf.Approximately(shapeDocument.BottomPadding, bp.y.positivePadding) ||
                !Mathf.Approximately(shapeDocument.BottomBorder, bp.y.positiveBorder);

            if (shapeChanged)
            {
                shapeDocument.LeftBorder = bp.x.negativeBorder;
                shapeDocument.LeftPadding = bp.x.negativePadding;
                shapeDocument.RightPadding = bp.x.positivePadding;
                shapeDocument.RightBorder = bp.x.positiveBorder;

                shapeDocument.TopBorder = bp.y.negativeBorder;
                shapeDocument.TopPadding = bp.y.negativePadding;
                shapeDocument.BottomPadding = bp.y.positivePadding;
                shapeDocument.BottomBorder = bp.y.positiveBorder;

                shapeDocument.MarkDirty();
                _forcePreviewRefresh = true;
            }

            bool profileChanged =
                !Mathf.Approximately(profileDocument.LeftPadding, bp.x.negativePadding) ||
                !Mathf.Approximately(profileDocument.RightPadding, bp.x.positivePadding) ||
                !Mathf.Approximately(profileDocument.TopPadding, bp.y.negativePadding) ||
                !Mathf.Approximately(profileDocument.BottomPadding, bp.y.positivePadding) ||
                !Mathf.Approximately(profileDocument.LeftBorder, bp.x.negativeBorder) ||
                !Mathf.Approximately(profileDocument.RightBorder, bp.x.positiveBorder) ||
                !Mathf.Approximately(profileDocument.TopBorder, bp.y.negativeBorder) ||
                !Mathf.Approximately(profileDocument.BottomBorder, bp.y.positiveBorder) ||
                !Mathf.Approximately(profileDocument.FrontPaddingDepth, Mathf.Max(bp.z.negativePadding, bp.z.positivePadding)) ||
                !Mathf.Approximately(profileDocument.FrontBorderDepth, Mathf.Max(bp.z.negativeBorder, bp.z.positiveBorder));

            if (profileChanged)
            {
                profileDocument.SetGuideValues(
                    bp.x.negativePadding,
                    bp.x.positivePadding,
                    bp.y.negativePadding,
                    bp.y.positivePadding,
                    bp.x.negativeBorder,
                    bp.x.positiveBorder,
                    bp.y.negativeBorder,
                    bp.y.positiveBorder);

                profileDocument.FrontPaddingDepth = Mathf.Max(bp.z.negativePadding, bp.z.positivePadding);
                profileDocument.FrontBorderDepth = Mathf.Max(bp.z.negativeBorder, bp.z.positiveBorder);
                profileDocument.MarkDirty();
                _forcePreviewRefresh = true;
            }
        }

        private void DrawSelectedShapeElementInspector()
""",
        text,
        "ShapeStamperWindow.cs",
    )

    write(WINDOW, text)
    print("Patched ShapeStamperWindow.cs")

def main() -> int:
    try:
        write_adaptive_shape()
        patch_window()
    except Exception as exc:
        print(f"Patch failed: {exc}", file=sys.stderr)
        return 1

    print("\\nDone.")
    print("Next steps:")
    print("1. Let Unity recompile.")
    print("2. Open Shape Stamper.")
    print("3. Select an object with AdaptiveShape and press 'Use Selected', or press 'Create Adaptive Shape'.")
    print("4. Edit SmartBounds borders/padding in the Inspector and confirm the window follows without fighting the Inspector.")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())