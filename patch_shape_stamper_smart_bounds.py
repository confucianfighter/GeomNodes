from pathlib import Path
import re
import sys

ROOT = Path.cwd()

WINDOW = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/ShapeStamperWindow.cs"
SHAPE_DOC = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Documents/ShapeCanvasDocument.cs"
GUIDES = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Core/CanvasGuideDrawing.cs"


def read(path: Path) -> str:
    if not path.exists():
        raise FileNotFoundError(f"Missing file: {path}")
    return path.read_text(encoding="utf-8")


def write(path: Path, text: str) -> None:
    path.write_text(text, encoding="utf-8", newline="\n")


def ensure_contains(text: str, needle: str, where: str) -> None:
    if needle not in text:
        raise RuntimeError(f"Expected to find {needle!r} in {where}, but did not.")


def patch_shape_canvas_document() -> None:
    text = read(SHAPE_DOC)

    if "private float leftPadding;" in text:
        print("ShapeCanvasDocument.cs already appears patched.")
        return

    anchor = """        [SerializeField] private bool hasInnerShape;
        [SerializeField] private List<CanvasPoint> innerPoints = new();
        [SerializeField] private List<CanvasEdge> innerEdges = new();

        [SerializeField, HideInInspector] private int revision;
"""
    replacement = """        [SerializeField] private bool hasInnerShape;
        [SerializeField] private List<CanvasPoint> innerPoints = new();
        [SerializeField] private List<CanvasEdge> innerEdges = new();

        [SerializeField] private float leftPadding;
        [SerializeField] private float rightPadding;
        [SerializeField] private float topPadding;
        [SerializeField] private float bottomPadding;

        [SerializeField] private float leftBorder;
        [SerializeField] private float rightBorder;
        [SerializeField] private float topBorder;
        [SerializeField] private float bottomBorder;

        [SerializeField, HideInInspector] private int revision;
"""
    ensure_contains(text, anchor, "ShapeCanvasDocument.cs")
    text = text.replace(anchor, replacement, 1)

    prop_anchor = """        public bool HasInnerShape
        {
            get => hasInnerShape;
            set
            {
                hasInnerShape = value;
                if (!hasInnerShape && editMode == ShapeLoopEditMode.Inner)
                    editMode = ShapeLoopEditMode.Outer;
            }
        }

        public Vector2 WorldSizeMeters
"""
    prop_replacement = """        public bool HasInnerShape
        {
            get => hasInnerShape;
            set
            {
                hasInnerShape = value;
                if (!hasInnerShape && editMode == ShapeLoopEditMode.Inner)
                    editMode = ShapeLoopEditMode.Outer;
            }
        }

        public float LeftPadding { get => leftPadding; set => leftPadding = Mathf.Max(0f, value); }
        public float RightPadding { get => rightPadding; set => rightPadding = Mathf.Max(0f, value); }
        public float TopPadding { get => topPadding; set => topPadding = Mathf.Max(0f, value); }
        public float BottomPadding { get => bottomPadding; set => bottomPadding = Mathf.Max(0f, value); }

        public float LeftBorder { get => leftBorder; set => leftBorder = Mathf.Max(0f, value); }
        public float RightBorder { get => rightBorder; set => rightBorder = Mathf.Max(0f, value); }
        public float TopBorder { get => topBorder; set => topBorder = Mathf.Max(0f, value); }
        public float BottomBorder { get => bottomBorder; set => bottomBorder = Mathf.Max(0f, value); }

        public Vector2 WorldSizeMeters
"""
    ensure_contains(text, prop_anchor, "ShapeCanvasDocument.cs")
    text = text.replace(prop_anchor, prop_replacement, 1)

    write(SHAPE_DOC, text)
    print("Patched ShapeCanvasDocument.cs")


def patch_shape_stamper_window() -> None:
    text = read(WINDOW)

    if "private DLN.SmartBounds _targetSmartBounds;" in text:
        print("ShapeStamperWindow.cs already appears patched.")
        return

    if "using DLN;" not in text:
        text = text.replace(
            "using System.Collections.Generic;\nusing UnityEditor;\nusing UnityEngine;\n",
            "using System.Collections.Generic;\nusing UnityEditor;\nusing UnityEngine;\nusing DLN;\n",
            1,
        )

    field_anchor = """        [SerializeField] private bool showMaterialSettings = true;
        [SerializeField] private bool autoRegeneratePreview = true;
        [SerializeField] private List<Material> segmentMaterials = new();
"""
    field_replacement = """        [SerializeField] private bool showMaterialSettings = true;
        [SerializeField] private bool autoRegeneratePreview = true;
        [SerializeField] private GameObject targetObject;
        [SerializeField] private List<Material> segmentMaterials = new();
"""
    ensure_contains(text, field_anchor, "ShapeStamperWindow.cs")
    text = text.replace(field_anchor, field_replacement, 1)

    private_field_anchor = """        private bool _isDraggingDivider;
        private int _lastShapeRevision = -1;
"""
    private_field_replacement = """        private bool _isDraggingDivider;
        private DLN.SmartBounds _targetSmartBounds;
        private int _lastShapeRevision = -1;
"""
    ensure_contains(text, private_field_anchor, "ShapeStamperWindow.cs")
    text = text.replace(private_field_anchor, private_field_replacement, 1)

    on_enable_anchor = """            shapeDocument.EnsureValidShape();
            profileDocument.EnsureValidProfile();

            _shapePolicy ??= new ShapeCanvasPolicy(shapeDocument);
"""
    on_enable_replacement = """            shapeDocument.EnsureValidShape();
            profileDocument.EnsureValidProfile();

            DLN.SmartBounds smartBounds = EnsureTargetSmartBounds();
            if (smartBounds != null)
                SyncDocumentsFromSmartBounds(smartBounds);

            _shapePolicy ??= new ShapeCanvasPolicy(shapeDocument);
"""
    ensure_contains(text, on_enable_anchor, "ShapeStamperWindow.cs")
    text = text.replace(on_enable_anchor, on_enable_replacement, 1)

    on_gui_anchor = """            shapeDocument.EnsureValidShape();
            profileDocument.EnsureValidProfile();

            DrawTopBar();
"""
    on_gui_replacement = """            shapeDocument.EnsureValidShape();
            profileDocument.EnsureValidProfile();

            DLN.SmartBounds smartBounds = EnsureTargetSmartBounds();
            if (smartBounds != null)
                SyncDocumentsFromSmartBounds(smartBounds);

            DrawTopBar();
"""
    ensure_contains(text, on_gui_anchor, "ShapeStamperWindow.cs")
    text = text.replace(on_gui_anchor, on_gui_replacement, 1)

    topbar_anchor = """        private void DrawTopBar()
        {
            EditorGUILayout.BeginVertical(GUILayout.Height(TopBarHeight));
            EditorGUILayout.Space(6f);

            EditorGUILayout.BeginHorizontal();
"""
    topbar_replacement = """        private void DrawTopBar()
        {
            EditorGUILayout.BeginVertical(GUILayout.Height(TopBarHeight));
            EditorGUILayout.Space(6f);

            EditorGUILayout.BeginHorizontal();
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

            EditorGUILayout.BeginHorizontal();
"""
    ensure_contains(text, topbar_anchor, "ShapeStamperWindow.cs")
    text = text.replace(topbar_anchor, topbar_replacement, 1)

    text = text.replace("DrawProfileGuideInputs();", "DrawBordersPaddingInputs();")

    old_method_pattern = re.compile(
        r"""        private void DrawProfileGuideInputs\(\)\n        \{\n.*?\n        \}\n\n        private void DrawSelectedShapeElementInspector\(\)""",
        re.DOTALL,
    )
    new_method_block = """        private void DrawBordersPaddingInputs()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Borders / Padding", EditorStyles.boldLabel);

            DLN.SmartBounds smartBounds = EnsureTargetSmartBounds();
            if (smartBounds == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a target GameObject. A SmartBounds component will be added automatically and used as the source of truth.",
                    MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            DLN.BordersPadding data = smartBounds.bordersPadding;

            EditorGUI.BeginChangeCheck();

            DrawAxisBordersPaddingRow("X", ref data.x);
            DrawAxisBordersPaddingRow("Y", ref data.y);
            DrawAxisBordersPaddingRow("Z", ref data.z);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(smartBounds, "Edit SmartBounds Borders/Padding");

                data.ClampToValid();
                smartBounds.bordersPadding = data;
                EditorUtility.SetDirty(smartBounds);

                SyncDocumentsFromSmartBounds(smartBounds);

                shapeDocument.MarkDirty();
                profileDocument.MarkDirty();
                _forcePreviewRefresh = true;
                Repaint();
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawAxisBordersPaddingRow(string axisLabel, ref DLN.AxisBordersPadding axis)
        {
            EditorGUILayout.LabelField(axisLabel, EditorStyles.miniBoldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("-B", GUILayout.Width(22f));
            axis.negativeBorder = EditorGUILayout.FloatField(axis.negativeBorder, GUILayout.Width(58f));

            EditorGUILayout.LabelField("-P", GUILayout.Width(22f));
            axis.negativePadding = EditorGUILayout.FloatField(axis.negativePadding, GUILayout.Width(58f));

            EditorGUILayout.LabelField("MinC", GUILayout.Width(34f));
            axis.minContentsSize = EditorGUILayout.FloatField(axis.minContentsSize, GUILayout.Width(58f));

            EditorGUILayout.LabelField("+P", GUILayout.Width(22f));
            axis.positivePadding = EditorGUILayout.FloatField(axis.positivePadding, GUILayout.Width(58f));

            EditorGUILayout.LabelField("+B", GUILayout.Width(22f));
            axis.positiveBorder = EditorGUILayout.FloatField(axis.positiveBorder, GUILayout.Width(58f));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private DLN.SmartBounds EnsureTargetSmartBounds()
        {
            if (targetObject == null)
            {
                _targetSmartBounds = null;
                return null;
            }

            if (_targetSmartBounds != null && _targetSmartBounds.gameObject == targetObject)
                return _targetSmartBounds;

            if (!targetObject.TryGetComponent(out DLN.SmartBounds smartBounds))
            {
                Undo.AddComponent<DLN.SmartBounds>(targetObject);
                smartBounds = targetObject.GetComponent<DLN.SmartBounds>();
            }

            _targetSmartBounds = smartBounds;
            return _targetSmartBounds;
        }

        private void SyncDocumentsFromSmartBounds(DLN.SmartBounds smartBounds)
        {
            if (smartBounds == null)
                return;

            DLN.BordersPadding bp = smartBounds.bordersPadding;
            bp.ClampToValid();

            shapeDocument.LeftBorder = bp.x.negativeBorder;
            shapeDocument.LeftPadding = bp.x.negativePadding;
            shapeDocument.RightPadding = bp.x.positivePadding;
            shapeDocument.RightBorder = bp.x.positiveBorder;

            shapeDocument.TopBorder = bp.y.negativeBorder;
            shapeDocument.TopPadding = bp.y.negativePadding;
            shapeDocument.BottomPadding = bp.y.positivePadding;
            shapeDocument.BottomBorder = bp.y.positiveBorder;

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
        }

        private void DrawSelectedShapeElementInspector()
"""
    if not old_method_pattern.search(text):
        raise RuntimeError("Could not locate DrawProfileGuideInputs() block in ShapeStamperWindow.cs")
    text = old_method_pattern.sub(new_method_block, text, count=1)

    write(WINDOW, text)
    print("Patched ShapeStamperWindow.cs")


def rewrite_canvas_guide_drawing() -> None:
    new_text = """#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    public static class CanvasGuideDrawing
    {
        private static readonly Color ContentColor = new Color(1f, 1f, 1f, 0.22f);
        private static readonly Color PaddingColor = new Color(0.35f, 0.85f, 1f, 0.30f);
        private static readonly Color BorderColor = new Color(1f, 0.65f, 0.20f, 0.30f);

        public static void DrawShapeGuides(EditorCanvas canvas, Rect canvasRect, ShapeCanvasDocument document)
        {
            if (canvas == null || document == null)
                return;

            float width = document.WorldSizeMeters.x;
            float height = document.WorldSizeMeters.y;

            float leftContent = 0f;
            float leftPadding = Mathf.Clamp(document.LeftPadding, 0f, width);
            float leftBorder = -(document.LeftPadding + document.LeftBorder);

            float rightContent = width;
            float rightPadding = Mathf.Clamp(width - document.RightPadding, 0f, width);
            float rightBorder = width + document.RightPadding + document.RightBorder;

            float topContent = 0f;
            float topPadding = Mathf.Clamp(document.TopPadding, 0f, height);
            float topBorder = -(document.TopPadding + document.TopBorder);

            float bottomContent = height;
            float bottomPadding = Mathf.Clamp(height - document.BottomPadding, 0f, height);
            float bottomBorder = height + document.BottomPadding + document.BottomBorder;

            DrawVerticalWorldLine(canvas, canvasRect, leftBorder, 0f, height, BorderColor);
            DrawVerticalWorldLine(canvas, canvasRect, leftContent, 0f, height, ContentColor);
            DrawVerticalWorldLine(canvas, canvasRect, leftPadding, 0f, height, PaddingColor);

            DrawVerticalWorldLine(canvas, canvasRect, rightBorder, 0f, height, BorderColor);
            DrawVerticalWorldLine(canvas, canvasRect, rightContent, 0f, height, ContentColor);
            DrawVerticalWorldLine(canvas, canvasRect, rightPadding, 0f, height, PaddingColor);

            DrawHorizontalWorldLine(canvas, canvasRect, 0f, width, topBorder, BorderColor);
            DrawHorizontalWorldLine(canvas, canvasRect, 0f, width, topContent, ContentColor);
            DrawHorizontalWorldLine(canvas, canvasRect, 0f, width, topPadding, PaddingColor);

            DrawHorizontalWorldLine(canvas, canvasRect, 0f, width, bottomBorder, BorderColor);
            DrawHorizontalWorldLine(canvas, canvasRect, 0f, width, bottomContent, ContentColor);
            DrawHorizontalWorldLine(canvas, canvasRect, 0f, width, bottomPadding, PaddingColor);

            DrawVerticalWorldLineLabel(canvas, canvasRect, leftBorder, 0f, "-X Border", BorderColor, placeRight: false);
            DrawVerticalWorldLineLabel(canvas, canvasRect, leftContent, 0f, "-X Content", ContentColor, placeRight: true);
            DrawVerticalWorldLineLabel(canvas, canvasRect, leftPadding, 0f, "-X Padding", PaddingColor, placeRight: true);

            DrawVerticalWorldLineLabel(canvas, canvasRect, rightBorder, 0f, "+X Border", BorderColor, placeRight: true);
            DrawVerticalWorldLineLabel(canvas, canvasRect, rightContent, 0f, "+X Content", ContentColor, placeRight: false);
            DrawVerticalWorldLineLabel(canvas, canvasRect, rightPadding, 0f, "+X Padding", PaddingColor, placeRight: false);

            DrawHorizontalWorldLineLabel(canvas, canvasRect, 0f, topBorder, "-Y Border", BorderColor, placeBelow: false);
            DrawHorizontalWorldLineLabel(canvas, canvasRect, 0f, topContent, "-Y Content", ContentColor, placeBelow: true);
            DrawHorizontalWorldLineLabel(canvas, canvasRect, 0f, topPadding, "-Y Padding", PaddingColor, placeBelow: true);

            DrawHorizontalWorldLineLabel(canvas, canvasRect, 0f, bottomBorder, "+Y Border", BorderColor, placeBelow: true);
            DrawHorizontalWorldLineLabel(canvas, canvasRect, 0f, bottomContent, "+Y Content", ContentColor, placeBelow: false);
            DrawHorizontalWorldLineLabel(canvas, canvasRect, 0f, bottomPadding, "+Y Padding", PaddingColor, placeBelow: false);
        }

        public static void DrawProfileGuides(EditorCanvas canvas, Rect canvasRect, ProfileCanvasDocument document)
        {
            if (canvas == null || document == null)
                return;

            float width = document.WorldSizeMeters.x;
            float height = document.WorldSizeMeters.y;

            float topContentY = 0f;
            float topPaddingY = Mathf.Clamp(document.TopPadding, 0f, height);
            float topBorderY = Mathf.Clamp(document.TopPadding + document.TopBorder, 0f, height);

            float bottomContentY = height;
            float bottomPaddingY = Mathf.Clamp(height - document.BottomPadding, 0f, height);
            float bottomBorderY = Mathf.Clamp(height - (document.BottomPadding + document.BottomBorder), 0f, height);

            DrawHorizontalWorldLine(canvas, canvasRect, 0f, width, topContentY, ContentColor);
            DrawHorizontalWorldLine(canvas, canvasRect, 0f, width, topPaddingY, PaddingColor);
            DrawHorizontalWorldLine(canvas, canvasRect, 0f, width, topBorderY, BorderColor);

            DrawHorizontalWorldLine(canvas, canvasRect, 0f, width, bottomContentY, ContentColor);
            DrawHorizontalWorldLine(canvas, canvasRect, 0f, width, bottomPaddingY, PaddingColor);
            DrawHorizontalWorldLine(canvas, canvasRect, 0f, width, bottomBorderY, BorderColor);

            DrawHorizontalWorldLineLabel(canvas, canvasRect, 0f, topContentY, "-Y Content", ContentColor, placeBelow: true);
            DrawHorizontalWorldLineLabel(canvas, canvasRect, 0f, topPaddingY, "-Y Padding", PaddingColor, placeBelow: true);
            DrawHorizontalWorldLineLabel(canvas, canvasRect, 0f, topBorderY, "-Y Border", BorderColor, placeBelow: false);

            DrawHorizontalWorldLineLabel(canvas, canvasRect, 0f, bottomContentY, "+Y Content", ContentColor, placeBelow: false);
            DrawHorizontalWorldLineLabel(canvas, canvasRect, 0f, bottomPaddingY, "+Y Padding", PaddingColor, placeBelow: false);
            DrawHorizontalWorldLineLabel(canvas, canvasRect, 0f, bottomBorderY, "+Y Border", BorderColor, placeBelow: true);

            float maxPaddingSpan = Mathf.Max(
                document.LeftPadding,
                document.RightPadding,
                document.TopPadding,
                document.BottomPadding,
                document.FrontPaddingDepth);

            float maxBorderOnly = Mathf.Max(
                document.LeftBorder,
                document.RightBorder,
                document.TopBorder,
                document.BottomBorder,
                document.FrontBorderDepth);

            float maxBorderSpan = maxPaddingSpan + maxBorderOnly;

            maxPaddingSpan = Mathf.Clamp(maxPaddingSpan, 0f, width * 0.5f);
            maxBorderSpan = Mathf.Clamp(maxBorderSpan, 0f, width * 0.5f);

            float negZContentX = 0f;
            float negZPaddingX = maxPaddingSpan;
            float negZBorderX = maxBorderSpan;

            float posZContentX = width;
            float posZPaddingX = width - maxPaddingSpan;
            float posZBorderX = width - maxBorderSpan;

            DrawVerticalWorldLine(canvas, canvasRect, negZContentX, 0f, height, ContentColor);
            DrawVerticalWorldLine(canvas, canvasRect, negZPaddingX, 0f, height, PaddingColor);
            DrawVerticalWorldLine(canvas, canvasRect, negZBorderX, 0f, height, BorderColor);

            DrawVerticalWorldLine(canvas, canvasRect, posZContentX, 0f, height, ContentColor);
            DrawVerticalWorldLine(canvas, canvasRect, posZPaddingX, 0f, height, PaddingColor);
            DrawVerticalWorldLine(canvas, canvasRect, posZBorderX, 0f, height, BorderColor);

            DrawVerticalWorldLineLabel(canvas, canvasRect, negZContentX, 0f, "-Z Content", ContentColor, placeRight: true);
            DrawVerticalWorldLineLabel(canvas, canvasRect, negZPaddingX, 0f, "-Z Padding", PaddingColor, placeRight: true);
            DrawVerticalWorldLineLabel(canvas, canvasRect, negZBorderX, 0f, "-Z Border", BorderColor, placeRight: true);

            DrawVerticalWorldLineLabel(canvas, canvasRect, posZContentX, 0f, "+Z Content", ContentColor, placeRight: false);
            DrawVerticalWorldLineLabel(canvas, canvasRect, posZPaddingX, 0f, "+Z Padding", PaddingColor, placeRight: false);
            DrawVerticalWorldLineLabel(canvas, canvasRect, posZBorderX, 0f, "+Z Border", BorderColor, placeRight: false);
        }

        private static void DrawHorizontalWorldLine(
            EditorCanvas canvas,
            Rect canvasRect,
            float xMin,
            float xMax,
            float y,
            Color color)
        {
            Vector2 a = CanvasMath.CanvasToScreen(new Vector2(xMin, y), canvasRect, canvas.View, canvas.Document);
            Vector2 b = CanvasMath.CanvasToScreen(new Vector2(xMax, y), canvasRect, canvas.View, canvas.Document);
            DrawLine(a, b, color);
        }

        private static void DrawVerticalWorldLine(
            EditorCanvas canvas,
            Rect canvasRect,
            float x,
            float yMin,
            float yMax,
            Color color)
        {
            Vector2 a = CanvasMath.CanvasToScreen(new Vector2(x, yMin), canvasRect, canvas.View, canvas.Document);
            Vector2 b = CanvasMath.CanvasToScreen(new Vector2(x, yMax), canvasRect, canvas.View, canvas.Document);
            DrawLine(a, b, color);
        }

        private static void DrawVerticalWorldLineLabel(
            EditorCanvas canvas,
            Rect canvasRect,
            float x,
            float y,
            string text,
            Color color,
            bool placeRight)
        {
            Vector2 top = CanvasMath.CanvasToScreen(new Vector2(x, y), canvasRect, canvas.View, canvas.Document);
            DrawVerticalLineLabel(top, text, color, placeRight);
        }

        private static void DrawHorizontalWorldLineLabel(
            EditorCanvas canvas,
            Rect canvasRect,
            float x,
            float y,
            string text,
            Color color,
            bool placeBelow)
        {
            Vector2 start = CanvasMath.CanvasToScreen(new Vector2(x, y), canvasRect, canvas.View, canvas.Document);
            DrawHorizontalLineLabel(start, text, color, placeBelow);
        }

        private static void DrawLine(Vector2 a, Vector2 b, Color color)
        {
            Handles.BeginGUI();
            Color old = Handles.color;
            Handles.color = color;
            Handles.DrawAAPolyLine(1.5f, a, b);
            Handles.color = old;
            Handles.EndGUI();
        }

        private static void DrawVerticalLineLabel(Vector2 lineTop, string text, Color color, bool placeRight, float yOffset = 6f)
        {
            GUIStyle style = new GUIStyle(EditorStyles.miniBoldLabel);
            style.normal.textColor = color;
            style.alignment = placeRight ? TextAnchor.UpperLeft : TextAnchor.UpperRight;

            Vector2 size = style.CalcSize(new GUIContent(text));
            float x = placeRight ? lineTop.x + 4f : lineTop.x - 4f;
            Rect rect = new Rect(x, lineTop.y + yOffset, size.x + 4f, size.y + 2f);

            if (!placeRight)
                rect.x -= rect.width;

            GUI.Label(rect, text, style);
        }

        private static void DrawHorizontalLineLabel(Vector2 lineStart, string text, Color color, bool placeBelow, float xOffset = 6f)
        {
            GUIStyle style = new GUIStyle(EditorStyles.miniBoldLabel);
            style.normal.textColor = color;
            style.alignment = TextAnchor.MiddleLeft;

            Vector2 size = style.CalcSize(new GUIContent(text));
            float y = placeBelow ? lineStart.y + 2f : lineStart.y - size.y - 2f;
            Rect rect = new Rect(lineStart.x + xOffset, y, size.x + 4f, size.y + 2f);
            GUI.Label(rect, text, style);
        }
    }
}
#endif
"""
    write(GUIDES, new_text)
    print("Rewrote CanvasGuideDrawing.cs")


def main() -> int:
    try:
        patch_shape_canvas_document()
        patch_shape_stamper_window()
        rewrite_canvas_guide_drawing()
    except Exception as exc:
        print(f"Patch failed: {exc}", file=sys.stderr)
        return 1

    print("\\nDone.")
    print("Next steps:")
    print("1. Let Unity recompile.")
    print("2. Open Shape Stamper.")
    print("3. Assign a target GameObject.")
    print("4. Verify SmartBounds gets auto-added.")
    print("5. Change X/Y/Z borders/padding in the new Borders / Padding section and confirm both overlays move.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())