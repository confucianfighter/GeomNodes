using UnityEditor;
using UnityEngine;
using DLN;

[CustomPropertyDrawer(typeof(CheckboxLeftAttribute))]
public class CheckboxLeftDrawer : PropertyDrawer
{
    private const float ToggleWidth = 18f;
    private const float Gap = 2f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var attr = (CheckboxLeftAttribute)attribute;

        if (property.propertyType != SerializedPropertyType.Boolean)
        {
            EditorGUI.LabelField(position, label.text, "CheckboxLeft only works on bool fields.");
            return;
        }

        EditorGUI.BeginProperty(position, label, property);

        Rect contentRect = new Rect(
            position.x + attr.indentAmount,
            position.y,
            Mathf.Max(0f, position.width - attr.indentAmount),
            EditorGUIUtility.singleLineHeight
        );

        Rect toggleRect = new Rect(
            contentRect.x,
            contentRect.y,
            ToggleWidth,
            contentRect.height
        );

        Rect labelRect = new Rect(
            contentRect.x + ToggleWidth + Gap,
            contentRect.y,
            Mathf.Max(0f, contentRect.width - ToggleWidth - Gap),
            contentRect.height
        );

        GUIContent drawLabel = string.IsNullOrEmpty(attr.overrideLabel)
            ? label
            : new GUIContent(attr.overrideLabel, label.tooltip);

        property.boolValue = EditorGUI.Toggle(toggleRect, property.boolValue);
        EditorGUI.LabelField(labelRect, drawLabel);

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
}