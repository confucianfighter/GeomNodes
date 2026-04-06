using UnityEditor;
using UnityEngine;
using DLN;

[CustomPropertyDrawer(typeof(ShowIfBoolAttribute))]
public class ShowIfBoolDrawer : PropertyDrawer
{
    private const float ToggleWidth = 18f;
    private const float ToggleLabelGap = 2f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var attr = (ShowIfBoolAttribute)attribute;

        if (!ShouldShow(property, attr))
            return;

        EditorGUI.BeginProperty(position, label, property);

        Rect contentRect = new Rect(
            position.x + attr.indentAmount,
            position.y,
            Mathf.Max(0f, position.width - attr.indentAmount),
            position.height
        );

        GUIContent drawLabel = string.IsNullOrEmpty(attr.overrideLabel)
            ? label
            : new GUIContent(attr.overrideLabel, label.tooltip);

        if (property.propertyType == SerializedPropertyType.Boolean)
        {
            DrawBoolFieldCheckboxFirst(contentRect, property, drawLabel);
        }
        else
        {
            EditorGUI.PropertyField(contentRect, property, drawLabel, true);
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var attr = (ShowIfBoolAttribute)attribute;

        if (!ShouldShow(property, attr))
            return 0f;

        if (property.propertyType == SerializedPropertyType.Boolean)
            return EditorGUIUtility.singleLineHeight;

        GUIContent drawLabel = string.IsNullOrEmpty(attr.overrideLabel)
            ? label
            : new GUIContent(attr.overrideLabel, label.tooltip);

        return EditorGUI.GetPropertyHeight(property, drawLabel, true);
    }

    private void DrawBoolFieldCheckboxFirst(Rect rect, SerializedProperty boolProp, GUIContent label)
    {
        Rect toggleRect = new Rect(
            rect.x,
            rect.y,
            ToggleWidth,
            EditorGUIUtility.singleLineHeight
        );

        Rect labelRect = new Rect(
            rect.x + ToggleWidth + ToggleLabelGap,
            rect.y,
            Mathf.Max(0f, rect.width - ToggleWidth - ToggleLabelGap),
            EditorGUIUtility.singleLineHeight
        );

        boolProp.boolValue = EditorGUI.Toggle(toggleRect, boolProp.boolValue);
        EditorGUI.LabelField(labelRect, label);
    }

    private bool ShouldShow(SerializedProperty property, ShowIfBoolAttribute attr)
    {
        if (attr == null)
            return true;

        if (string.IsNullOrEmpty(attr.controllingBoolFieldName))
            return true;

        SerializedProperty boolProp = FindControllingProperty(property, attr.controllingBoolFieldName);

        if (boolProp == null)
            return true;

        if (boolProp.propertyType != SerializedPropertyType.Boolean)
            return true;

        return boolProp.boolValue == attr.showIf;
    }

    private SerializedProperty FindControllingProperty(SerializedProperty property, string controllingFieldName)
    {
        if (property == null || string.IsNullOrEmpty(controllingFieldName))
            return null;

        SerializedProperty rootProp = property.serializedObject.FindProperty(controllingFieldName);
        if (rootProp != null)
            return rootProp;

        string propertyPath = property.propertyPath;
        int lastDotIndex = propertyPath.LastIndexOf('.');

        if (lastDotIndex < 0)
            return null;

        string parentPath = propertyPath.Substring(0, lastDotIndex);
        string siblingPath = parentPath + "." + controllingFieldName;

        return property.serializedObject.FindProperty(siblingPath);
    }
}