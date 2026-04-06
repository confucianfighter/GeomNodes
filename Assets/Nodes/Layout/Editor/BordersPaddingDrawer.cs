using UnityEditor;
using UnityEngine;
using DLN;

namespace DLN.Editor
{
    [CustomPropertyDrawer(typeof(BordersPadding))]
    public class BordersPaddingDrawer : PropertyDrawer
    {
        private const float AxisLabelWidth = 24f;

        private const float FieldWidth = 56f;
        private const float FieldGap = 4f;
        private const float GroupGap = 8f;

        private const float VerticalHeaderPadding = 4f;
        private const float VerticalHeaderExtraWidth = 6f;

        private static readonly string[] HeaderLabels =
        {
            "Border",
            "Padding",
            "Min",
            "Padding",
            "Border"
        };

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            float height = line; // foldout

            if (!property.isExpanded)
                return height;

            float headerHeight = GetVerticalHeaderHeight();

            // Header + X + Y + Z
            height += spacing + headerHeight;
            height += spacing + line;
            height += spacing + line;
            height += spacing + line;

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
            float headerHeight = GetVerticalHeaderHeight();

            Rect contentRect = EditorGUI.IndentedRect(
                new Rect(position.x, y, position.width, position.height - y));

            Rect headerRect = new Rect(contentRect.x, y, contentRect.width, headerHeight);
            DrawVerticalHeader(headerRect);

            y += headerHeight + spacing;

            Rect xRowRect = new Rect(contentRect.x, y, contentRect.width, line);
            DrawAxisRow(xRowRect, xProp, "X");
            y += line + spacing;

            Rect yRowRect = new Rect(contentRect.x, y, contentRect.width, line);
            DrawAxisRow(yRowRect, yProp, "Y");
            y += line + spacing;

            Rect zRowRect = new Rect(contentRect.x, y, contentRect.width, line);
            DrawAxisRow(zRowRect, zProp, "Z");

            EditorGUI.EndProperty();
        }

        private void DrawVerticalHeader(Rect rect)
        {
            Rect labelRect = new Rect(rect.x, rect.y, AxisLabelWidth, rect.height);
            EditorGUI.LabelField(labelRect, GUIContent.none); // intentional blank

            float x = rect.x + AxisLabelWidth;

            DrawVerticalLabel(ref x, rect, "Border");
            DrawVerticalLabel(ref x, rect, "Padding");

            x += GroupGap;

            DrawVerticalLabel(ref x, rect, "Min");

            x += GroupGap;

            DrawVerticalLabel(ref x, rect, "Padding");
            DrawVerticalLabel(ref x, rect, "Border");
        }

        private void DrawAxisRow(Rect rect, SerializedProperty axisProp, string axisLabel)
        {
            Rect labelRect = new Rect(rect.x, rect.y, AxisLabelWidth, rect.height);
            EditorGUI.LabelField(labelRect, axisLabel);

            float x = rect.x + AxisLabelWidth;

            DrawFloatField(ref x, rect.y, axisProp.FindPropertyRelative("negativeBorder"));
            DrawFloatField(ref x, rect.y, axisProp.FindPropertyRelative("negativePadding"));

            x += GroupGap;

            DrawFloatField(ref x, rect.y, axisProp.FindPropertyRelative("minContentsSize"));

            x += GroupGap;

            DrawFloatField(ref x, rect.y, axisProp.FindPropertyRelative("positivePadding"));
            DrawFloatField(ref x, rect.y, axisProp.FindPropertyRelative("positiveBorder"));
        }

        private void DrawFloatField(ref float x, float y, SerializedProperty prop)
        {
            Rect r = new Rect(x, y, FieldWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(r, prop, GUIContent.none);
            x += FieldWidth + FieldGap;
        }

        private void DrawVerticalLabel(ref float x, Rect headerRect, string text)
        {
            Rect columnRect = new Rect(x, headerRect.y, FieldWidth, headerRect.height);
            DrawRotatedCenteredLabel(columnRect, text);
            x += FieldWidth + FieldGap;
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

            // Rotated text width becomes vertical height requirement.
            return maxWidth + VerticalHeaderPadding + VerticalHeaderExtraWidth;
        }
    }
}