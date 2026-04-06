using UnityEditor;
using UnityEngine;
using DLN;

[CustomPropertyDrawer(typeof(ShowIfObjectAssignedAttribute))]
public class ShowIfObjectAssignedDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var attr = (ShowIfObjectAssignedAttribute)attribute;

        if (!IsControllingObjectAssigned(property, attr))
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
        var attr = (ShowIfObjectAssignedAttribute)attribute;

        if (!IsControllingObjectAssigned(property, attr))
            return 0f;

        GUIContent drawLabel = string.IsNullOrEmpty(attr.overrideLabel)
            ? label
            : new GUIContent(attr.overrideLabel, label.tooltip);

        return EditorGUI.GetPropertyHeight(property, drawLabel, true);
    }

    private bool IsControllingObjectAssigned(SerializedProperty property, ShowIfObjectAssignedAttribute attr)
    {
        if (attr == null || string.IsNullOrEmpty(attr.controllingObjectFieldName))
            return true;

        SerializedProperty controllingProp = property.serializedObject.FindProperty(attr.controllingObjectFieldName);

        if (controllingProp == null)
            return true;

        if (controllingProp.propertyType != SerializedPropertyType.ObjectReference)
            return true;

        return controllingProp.objectReferenceValue != null;
    }
}