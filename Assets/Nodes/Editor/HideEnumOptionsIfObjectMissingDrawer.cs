using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DLN.Editor
{
    [CustomPropertyDrawer(typeof(HideEnumOptionsIfObjectMissingAttribute))]
    public class HideEnumOptionsIfObjectMissingDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (HideEnumOptionsIfObjectMissingAttribute)attribute;

            if (property.propertyType != SerializedPropertyType.Enum)
            {
                EditorGUI.LabelField(position, label.text, "Use HideEnumOptionsIfObjectMissing only on enum fields.");
                return;
            }

            Rect drawRect = position;
            drawRect.x += attr.indentAmount;
            drawRect.width -= attr.indentAmount;

            SerializedProperty controllingProp =
                SerializedPropertyPathUtility.FindNearbyOrAncestorProperty(property, attr.controllingObjectFieldName);

            bool controllingObjectExists = HasObjectReference(controllingProp);

            if (controllingObjectExists)
            {
                EditorGUI.PropertyField(drawRect, property, label);
                return;
            }

            DrawFilteredEnumPopup(drawRect, property, label, attr);
        }

        private static bool HasObjectReference(SerializedProperty property)
        {
            if (property == null)
                return false;

            return property.propertyType == SerializedPropertyType.ObjectReference &&
                   property.objectReferenceValue != null;
        }

        private static void DrawFilteredEnumPopup(
            Rect position,
            SerializedProperty property,
            GUIContent label,
            HideEnumOptionsIfObjectMissingAttribute attr)
        {
            string[] enumNames = property.enumNames;
            string[] enumDisplayNames = property.enumDisplayNames;

            var hiddenNameSet = new HashSet<string>(attr.hiddenEnumValueNames ?? Array.Empty<string>());

            List<int> visibleIndices = new List<int>();
            List<GUIContent> visibleOptions = new List<GUIContent>();

            for (int i = 0; i < enumNames.Length; i++)
            {
                if (hiddenNameSet.Contains(enumNames[i]))
                    continue;

                visibleIndices.Add(i);
                visibleOptions.Add(new GUIContent(enumDisplayNames[i]));
            }

            int currentEnumIndex = property.enumValueIndex;
            bool currentValueIsHidden =
                currentEnumIndex >= 0 &&
                currentEnumIndex < enumNames.Length &&
                hiddenNameSet.Contains(enumNames[currentEnumIndex]);

            int popupIndex = -1;

            if (currentValueIsHidden)
            {
                visibleOptions.Insert(0, new GUIContent(attr.invalidValueLabel));
                popupIndex = 0;
            }
            else
            {
                popupIndex = visibleIndices.IndexOf(currentEnumIndex);
                if (popupIndex < 0 && visibleOptions.Count > 0)
                    popupIndex = 0;
            }

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();

            int newPopupIndex = EditorGUI.Popup(position, label, popupIndex, visibleOptions.ToArray());

            if (EditorGUI.EndChangeCheck())
            {
                if (currentValueIsHidden)
                {
                    if (newPopupIndex > 0)
                        property.enumValueIndex = visibleIndices[newPopupIndex - 1];
                }
                else
                {
                    if (newPopupIndex >= 0 && newPopupIndex < visibleIndices.Count)
                        property.enumValueIndex = visibleIndices[newPopupIndex];
                }
            }

            EditorGUI.EndProperty();
        }
    }
}