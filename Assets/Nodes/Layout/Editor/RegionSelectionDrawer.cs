using UnityEditor;
using UnityEngine;

namespace DLN.Editor
{
    [CustomPropertyDrawer(typeof(RegionSelection))]
    public class RegionSelectionDrawer : PropertyDrawer
    {
        private const float AxisLabelWidth = 24f;
        private const float LerpLabelWidth = 50f;

        private const float ToggleWidth = 18f;
        private const float ToggleGap = 2f;
        private const float GroupGap = 8f;

        private const float VerticalHeaderPadding = 4f;
        private const float VerticalHeaderExtraWidth = 6f;
        private const float SideLabelWidth = 22f;

        private static readonly string[] HeaderLabels =
        {
            "BorderEdge",
            "ContentEdge",
            "PaddingEdge",
            "Center",
            "PaddingEdge",
            "ContentEdge",
            "BorderEdge"
        };

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            float height = line; // foldout

            if (!property.isExpanded)
                return height;

            float verticalHeaderHeight = GetVerticalHeaderHeight();

            // Header + X + XLerp + Y + YLerp + Z + ZLerp
            height += spacing + verticalHeaderHeight;
            height += spacing + line; // X row
            height += spacing + line; // XLerp row
            height += spacing + line; // Y row
            height += spacing + line; // YLerp row
            height += spacing + line; // Z row
            height += spacing + line; // ZLerp row

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            Rect foldoutRect = new Rect(position.x, position.y, position.width, line);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            SerializedProperty xProp = property.FindPropertyRelative("x");
            SerializedProperty yProp = property.FindPropertyRelative("y");
            SerializedProperty zProp = property.FindPropertyRelative("z");

            float y = foldoutRect.yMax + spacing;
            float verticalHeaderHeight = GetVerticalHeaderHeight();

            Rect contentRect = EditorGUI.IndentedRect(new Rect(position.x, y, position.width, position.height - y));

            // Header above first row only
            Rect headerRect = new Rect(contentRect.x, y, contentRect.width, verticalHeaderHeight);
            DrawVerticalHeader(headerRect);

            y += verticalHeaderHeight + spacing;

            Rect xRowRect = new Rect(contentRect.x, y, contentRect.width, line);
            DrawAxisRow(xRowRect, xProp, "X");
            y += line + spacing;

            Rect xLerpRect = new Rect(contentRect.x, y, contentRect.width, line);
            DrawLerpRow(xLerpRect, xProp, "X Subselection:");
            y += line + spacing;

            Rect yRowRect = new Rect(contentRect.x, y, contentRect.width, line);
            DrawAxisRow(yRowRect, yProp, "Y");
            y += line + spacing;

            Rect yLerpRect = new Rect(contentRect.x, y, contentRect.width, line);
            DrawLerpRow(yLerpRect, yProp, "Y SubSelection:");
            y += line + spacing;

            Rect zRowRect = new Rect(contentRect.x, y, contentRect.width, line);
            DrawAxisRow(zRowRect, zProp, "Z");
            y += line + spacing;

            Rect zLerpRect = new Rect(contentRect.x, y, contentRect.width, line);
            DrawLerpRow(zLerpRect, zProp, "Z Subselection:");

            EditorGUI.EndProperty();
        }

        private void DrawVerticalHeader(Rect rect)
        {
            Rect labelRect = new Rect(rect.x, rect.y, AxisLabelWidth, rect.height);
            // left blank intentionally so header aligns with toggle columns
            EditorGUI.LabelField(labelRect, GUIContent.none);

            float x = rect.x + AxisLabelWidth;

            DrawVerticalLabel(ref x, rect, "BorderEdge");
            DrawVerticalLabel(ref x, rect, "ContentEdge");
            DrawVerticalLabel(ref x, rect, "PaddingEdge");

            x += GroupGap;

            DrawVerticalLabel(ref x, rect, "Center");

            x += GroupGap;

            DrawVerticalLabel(ref x, rect, "PaddingEdge");
            DrawVerticalLabel(ref x, rect, "ContentEdge");
            DrawVerticalLabel(ref x, rect, "BorderEdge");
        }

        private void DrawVerticalLabel(ref float x, Rect headerRect, string text)
        {
            Rect columnRect = new Rect(x, headerRect.y, ToggleWidth, headerRect.height);
            DrawRotatedCenteredLabel(columnRect, text);
            x += ToggleWidth + ToggleGap;
        }

        private void DrawRotatedCenteredLabel(Rect rect, string text)
        {
            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;

            GUIStyle style = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Overflow
            };

            Vector2 size = style.CalcSize(new GUIContent(text));

            // We rotate around the bottom-left of the pivot rect after translation.
            // Use a rect wide enough for the horizontal text before rotation.
            Vector2 pivot = new Vector2(rect.x + rect.width * 0.5f, rect.y + rect.height * 0.5f);

            GUIUtility.RotateAroundPivot(-90f, pivot);

            Rect rotatedRect = new Rect(
                pivot.x - size.x * 0.5f,
                pivot.y - EditorGUIUtility.singleLineHeight * 0.5f,
                size.x,
                EditorGUIUtility.singleLineHeight
            );

            EditorGUI.LabelField(rotatedRect, text, style);

            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }

        private void DrawAxisRow(Rect rect, SerializedProperty axisProp, string axisLabel)
        {
            Rect negativeLabelRect = new Rect(rect.x, rect.y, SideLabelWidth, rect.height);
            EditorGUI.LabelField(negativeLabelRect, "-" + axisLabel);

            float x = negativeLabelRect.xMax + 2f;

            DrawToggle(ref x, rect.y, axisProp.FindPropertyRelative("negativeBorderEdge"));
            DrawToggle(ref x, rect.y, axisProp.FindPropertyRelative("negativeContentEdge"));
            DrawToggle(ref x, rect.y, axisProp.FindPropertyRelative("negativePaddingEdge"));

            x += GroupGap;

            DrawToggle(ref x, rect.y, axisProp.FindPropertyRelative("center"));

            x += GroupGap;

            DrawToggle(ref x, rect.y, axisProp.FindPropertyRelative("positivePaddingEdge"));
            DrawToggle(ref x, rect.y, axisProp.FindPropertyRelative("positiveContentEdge"));
            DrawToggle(ref x, rect.y, axisProp.FindPropertyRelative("positiveBorderEdge"));

            Rect positiveLabelRect = new Rect(x, rect.y, SideLabelWidth, rect.height);
            EditorGUI.LabelField(positiveLabelRect, "+" + axisLabel);
        }
        private void DrawToggle(ref float x, float y, SerializedProperty prop)
        {
            Rect r = new Rect(x, y, ToggleWidth, EditorGUIUtility.singleLineHeight);
            prop.boolValue = EditorGUI.Toggle(r, GUIContent.none, prop.boolValue);
            x += ToggleWidth + ToggleGap;
        }

        private void DrawLerpRow(Rect rect, SerializedProperty axisProp, string label)
        {
            SerializedProperty minProp = axisProp.FindPropertyRelative("interpMin");
            SerializedProperty maxProp = axisProp.FindPropertyRelative("interpMax");

            const float rowLabelWidth = 110f;
            const float miniLabelWidth = 30f;
            const float gap = 6f;

            Rect rowLabelRect = new Rect(rect.x, rect.y, rowLabelWidth, rect.height);
            EditorGUI.LabelField(rowLabelRect, label);

            float x = rowLabelRect.xMax + gap;
            float remainingWidth = rect.xMax - x;

            float fieldWidth = (remainingWidth - gap - miniLabelWidth * 2f) * 0.5f;

            Rect startLabelRect = new Rect(x, rect.y, miniLabelWidth + gap, rect.height);
            x += miniLabelWidth;
            Rect startFieldRect = new Rect(x, rect.y, fieldWidth, rect.height);
            x += fieldWidth + gap;

            Rect endLabelRect = new Rect(x, rect.y, miniLabelWidth, rect.height);
            x += miniLabelWidth;
            Rect endFieldRect = new Rect(x, rect.y, fieldWidth, rect.height);

            EditorGUI.LabelField(startLabelRect, "Start");
            EditorGUI.PropertyField(startFieldRect, minProp, GUIContent.none);

            EditorGUI.LabelField(endLabelRect, "End");
            EditorGUI.PropertyField(endFieldRect, maxProp, GUIContent.none);
        }

        private float GetVerticalHeaderHeight()
        {
            GUIStyle style = EditorStyles.miniLabel;

            float maxWidth = 0f;
            for (int i = 0; i < HeaderLabels.Length; i++)
            {
                Vector2 size = style.CalcSize(new GUIContent(HeaderLabels[i]));
                if (size.x > maxWidth)
                    maxWidth = size.x;
            }

            // Because rotated width becomes height.
            return maxWidth + VerticalHeaderPadding + VerticalHeaderExtraWidth;
        }
    }
}