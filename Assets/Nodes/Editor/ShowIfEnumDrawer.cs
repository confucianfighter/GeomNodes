using UnityEditor;
using UnityEngine;
using DLN;

[CustomPropertyDrawer(typeof(ShowIfEnumAttribute))]
public class ShowIfEnumDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var attr = (ShowIfEnumAttribute)attribute;

        if (!DoesEnumMatch(property, attr))
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

        EditorGUI.PropertyField(contentRect, property, drawLabel, true);

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var attr = (ShowIfEnumAttribute)attribute;

        if (!DoesEnumMatch(property, attr))
            return 0f;

        GUIContent drawLabel = string.IsNullOrEmpty(attr.overrideLabel)
            ? label
            : new GUIContent(attr.overrideLabel, label.tooltip);

        return EditorGUI.GetPropertyHeight(property, drawLabel, true);
    }

    private bool DoesEnumMatch(SerializedProperty property, ShowIfEnumAttribute attr)
    {
        if (attr == null)
            return true;

        if (string.IsNullOrEmpty(attr.controllingEnumFieldName))
            return true;

        if (attr.requiredEnumValueNames == null || attr.requiredEnumValueNames.Length == 0)
            return true;

        SerializedProperty enumProp = FindControllingProperty(property, attr.controllingEnumFieldName);

        if (enumProp == null)
            return true;

        if (enumProp.propertyType != SerializedPropertyType.Enum)
            return true;

        int index = enumProp.enumValueIndex;
        string[] enumNames = enumProp.enumNames;

        if (index < 0 || index >= enumNames.Length)
            return true;

        string currentEnumName = enumNames[index];

        for (int i = 0; i < attr.requiredEnumValueNames.Length; i++)
        {
            if (currentEnumName == attr.requiredEnumValueNames[i])
                return true;
        }

        return false;
    }

    private SerializedProperty FindControllingProperty(SerializedProperty property, string controllingFieldName)
    {
        if (property == null || string.IsNullOrEmpty(controllingFieldName))
            return null;

        // 1. Try root-level lookup first.
        SerializedProperty rootProp = property.serializedObject.FindProperty(controllingFieldName);
        if (rootProp != null)
            return rootProp;

        // 2. Try sibling lookup relative to the current property's parent path.
        string propertyPath = property.propertyPath;
        int lastDotIndex = propertyPath.LastIndexOf('.');

        if (lastDotIndex < 0)
            return null;

        string parentPath = propertyPath.Substring(0, lastDotIndex);
        string siblingPath = parentPath + "." + controllingFieldName;

        SerializedProperty siblingProp = property.serializedObject.FindProperty(siblingPath);
        if (siblingProp != null)
            return siblingProp;

        return null;
    }
}