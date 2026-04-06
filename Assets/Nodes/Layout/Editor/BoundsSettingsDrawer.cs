using UnityEditor;
using UnityEngine;

namespace DLN.Editor
{
    [CustomPropertyDrawer(typeof(OptionalBoundsSettings))]
    public class BoundsSettingsDrawer : PropertyDrawer
    {
        private const float ToggleWidth = 18f;
        private const float LineButtonWidth = 60f;
        private const float SectionGap = 4f;
        private const float PopupWidth = 90f;
        private const float LabelGap = 6f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            float height = line; // foldout only

            if (!property.isExpanded)
                return height;

            height += spacing + line; // includeSelf
            height += spacing + line; // includeChildren
            height += spacing + line; // includeSelfEvenIfEmpty
            height += spacing + line; // useProxy
            height += spacing + line; // region selection enable row

            SerializedProperty regionSelectionProp = property.FindPropertyRelative("regionSelection");
            SerializedProperty regionHasValueProp = regionSelectionProp.FindPropertyRelative("hasValue");
            SerializedProperty regionValueProp = regionSelectionProp.FindPropertyRelative("value");

            if (regionHasValueProp.boolValue ||
                regionHasValueProp.hasMultipleDifferentValues)
            {
                height += spacing + EditorGUI.GetPropertyHeight(regionValueProp, true);
            }
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

            Rect contentRect = EditorGUI.IndentedRect(new Rect(
                position.x,
                foldoutRect.yMax + spacing,
                position.width,
                position.height - line - spacing));

            float y = contentRect.y;

            DrawOptionalBoolRow(
                new Rect(contentRect.x, y, contentRect.width, line),
                property.FindPropertyRelative("includeSelf"),
                "Include Self");
            y += line + spacing;

            DrawOptionalBoolRow(
                new Rect(contentRect.x, y, contentRect.width, line),
                property.FindPropertyRelative("includeChildren"),
                "Include Children");
            y += line + spacing;

            DrawOptionalBoolRow(
                new Rect(contentRect.x, y, contentRect.width, line),
                property.FindPropertyRelative("includeSelfEvenIfEmpty"),
                "Include Self Even If Empty");
            y += line + spacing;

            DrawOptionalBoolRow(
                new Rect(contentRect.x, y, contentRect.width, line),
                property.FindPropertyRelative("useProxy"),
                "Use Proxy");
            y += line + spacing + SectionGap;

            float regionHeight = GetOptionalRegionSelectionHeight(property.FindPropertyRelative("regionSelection"));
            DrawOptionalRegionSelection(
                new Rect(contentRect.x, y, contentRect.width, regionHeight),
                property.FindPropertyRelative("regionSelection"),
                "Region Selection");

            EditorGUI.EndProperty();
        }

        private void DrawOptionalBoolRow(Rect rect, SerializedProperty optionalProp, string label)
        {
            SerializedProperty hasValueProp = optionalProp.FindPropertyRelative("hasValue");
            SerializedProperty valueProp = optionalProp.FindPropertyRelative("value");

            Rect popupRect = new Rect(rect.x, rect.y, PopupWidth, rect.height);
            Rect labelRect = new Rect(
                popupRect.xMax + LabelGap,
                rect.y,
                rect.width - PopupWidth - LabelGap,
                rect.height);

            int state = 0;
            if (!hasValueProp.hasMultipleDifferentValues && hasValueProp.boolValue)
                state = valueProp.boolValue ? 1 : 2;
            bool mixed = hasValueProp.hasMultipleDifferentValues ||
             (!hasValueProp.hasMultipleDifferentValues &&
              hasValueProp.boolValue &&
              valueProp.hasMultipleDifferentValues);

            EditorGUI.showMixedValue = mixed;
            EditorGUI.BeginChangeCheck();

            state = EditorGUI.Popup(popupRect, state, new[] { "Not Set", "True", "False" });

            if (EditorGUI.EndChangeCheck())
            {
                switch (state)
                {
                    case 0:
                        hasValueProp.boolValue = false;
                        break;
                    case 1:
                        hasValueProp.boolValue = true;
                        valueProp.boolValue = true;
                        break;
                    case 2:
                        hasValueProp.boolValue = true;
                        valueProp.boolValue = false;
                        break;
                }
            }

            EditorGUI.showMixedValue = false;

            bool isInheritedForAll =
                !hasValueProp.hasMultipleDifferentValues &&
                !hasValueProp.boolValue;

            using (new EditorGUI.DisabledScope(isInheritedForAll))
            {
                EditorGUI.LabelField(labelRect, label);
            }
            isInheritedForAll =
                !hasValueProp.hasMultipleDifferentValues &&
                !hasValueProp.boolValue;

            if (isInheritedForAll)
            {
                Rect inheritedRect = new Rect(rect.xMax - LineButtonWidth, rect.y, LineButtonWidth, rect.height);
                GUIStyle mini = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleRight
                };
                EditorGUI.LabelField(inheritedRect, "Inherited", mini);
            }
        }

        private float GetOptionalRegionSelectionHeight(SerializedProperty optionalProp)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            float height = line; // enable row

            SerializedProperty hasValueProp = optionalProp.FindPropertyRelative("hasValue");
            SerializedProperty valueProp = optionalProp.FindPropertyRelative("value");

            if (hasValueProp.boolValue || hasValueProp.hasMultipleDifferentValues)
            {
                height += spacing + EditorGUI.GetPropertyHeight(valueProp, true);
            }

            return height;
        }

        private void DrawOptionalRegionSelection(Rect rect, SerializedProperty optionalProp, string label)
        {
            SerializedProperty hasValueProp = optionalProp.FindPropertyRelative("hasValue");
            SerializedProperty valueProp = optionalProp.FindPropertyRelative("value");

            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            Rect rowRect = new Rect(rect.x, rect.y, rect.width, line);

            Rect toggleRect = new Rect(rowRect.x, rowRect.y, ToggleWidth, rowRect.height);
            Rect labelRect = new Rect(toggleRect.xMax + 4f, rowRect.y, rowRect.width - ToggleWidth - 4f, rowRect.height);

            bool enabled = hasValueProp.hasMultipleDifferentValues ? false : hasValueProp.boolValue;

            EditorGUI.showMixedValue = hasValueProp.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            enabled = EditorGUI.Toggle(toggleRect, enabled);
            if (EditorGUI.EndChangeCheck())
            {
                hasValueProp.boolValue = enabled;
            }
            EditorGUI.showMixedValue = false;

            EditorGUI.LabelField(labelRect, label);

            bool showRegionChild = hasValueProp.boolValue || hasValueProp.hasMultipleDifferentValues;

            if (!showRegionChild)
            {
                Rect inheritedRect = new Rect(rowRect.xMax - LineButtonWidth, rowRect.y, LineButtonWidth, rowRect.height);
                GUIStyle mini = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleRight
                };
                EditorGUI.LabelField(inheritedRect, "Not Set", mini);
                return;
            }

            float childHeight = EditorGUI.GetPropertyHeight(valueProp, true);

            Rect childRect = new Rect(
                EditorGUI.IndentedRect(rowRect).x,
                rowRect.yMax + spacing,
                EditorGUI.IndentedRect(rowRect).width,
                childHeight);

            EditorGUI.PropertyField(childRect, valueProp, GUIContent.none, true);
        }
    }
}