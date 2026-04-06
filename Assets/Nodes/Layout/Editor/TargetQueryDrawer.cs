// using UnityEditor;
// using UnityEngine;

// namespace DLN.Editor
// {
//     [CustomPropertyDrawer(typeof(TargetQuery))]
//     public class TargetQueryDrawer : PropertyDrawer
//     {
//         private const float VerticalSectionSpacing = 2f;

//         public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
//         {
//             float line = EditorGUIUtility.singleLineHeight;
//             float spacing = EditorGUIUtility.standardVerticalSpacing;
//             float height = line;

//             if (!property.isExpanded)
//                 return height;

//             SerializedProperty rootProp = property.FindPropertyRelative("root");
//             SerializedProperty includeProp = property.FindPropertyRelative("include");
//             SerializedProperty labelProp = property.FindPropertyRelative("label");
//             SerializedProperty boundsOverridesProp = property.FindPropertyRelative("boundsOverrides");

//             height += spacing + EditorGUI.GetPropertyHeight(rootProp, includeChildren: true);
//             height += spacing + EditorGUI.GetPropertyHeight(includeProp, includeChildren: true);

//             TargetIncludeMode includeMode = (TargetIncludeMode)includeProp.enumValueIndex;

//             if (ShowsLabel(includeMode))
//             {
//                 height += spacing + EditorGUI.GetPropertyHeight(labelProp, includeChildren: true);

//                 if (RequiresLabel(includeMode) && string.IsNullOrWhiteSpace(labelProp.stringValue))
//                 {
//                     height += spacing + GetHelpBoxHeight("A non-empty label is required for this include mode.");
//                 }
//             }

//             height += spacing + VerticalSectionSpacing;
//             height += EditorGUI.GetPropertyHeight(boundsOverridesProp, includeChildren: true);

//             return height;
//         }

//         public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
//         {
//             EditorGUI.BeginProperty(position, label, property);

//             float line = EditorGUIUtility.singleLineHeight;
//             float spacing = EditorGUIUtility.standardVerticalSpacing;

//             SerializedProperty rootProp = property.FindPropertyRelative("root");
//             SerializedProperty includeProp = property.FindPropertyRelative("include");
//             SerializedProperty labelProp = property.FindPropertyRelative("label");
//             SerializedProperty boundsOverridesProp = property.FindPropertyRelative("boundsOverrides");

//             Rect rect = new Rect(position.x, position.y, position.width, line);

//             GUIContent foldoutLabel = GetFoldoutLabel(property, label);

//             property.isExpanded = EditorGUI.Foldout(
//                 rect,
//                 property.isExpanded,
//                 foldoutLabel,
//                 true);

//             if (!property.isExpanded)
//             {
//                 EditorGUI.EndProperty();
//                 return;
//             }

//             rect.y += line + spacing;

//             EditorGUI.indentLevel++;

//             DrawPropertyRow(ref rect, rootProp, "Root");
//             DrawPropertyRow(ref rect, includeProp, "Include");

//             TargetIncludeMode includeMode = (TargetIncludeMode)includeProp.enumValueIndex;

//             if (ShowsLabel(includeMode))
//             {
//                 string labelFieldName = RequiresLabel(includeMode)
//                     ? "Label"
//                     : "Optional Label";

//                 DrawPropertyRow(ref rect, labelProp, labelFieldName);

//                 if (RequiresLabel(includeMode) && string.IsNullOrWhiteSpace(labelProp.stringValue))
//                 {
//                     float helpHeight = GetHelpBoxHeight("A non-empty label is required for this include mode.");

//                     Rect helpRect = EditorGUI.IndentedRect(new Rect(
//                         rect.x,
//                         rect.y,
//                         rect.width,
//                         helpHeight));

//                     EditorGUI.HelpBox(
//                         helpRect,
//                         "A non-empty label is required for this include mode.",
//                         MessageType.Warning);

//                     rect.y += helpHeight + spacing;
//                 }
//             }

//             rect.y += VerticalSectionSpacing;

//             DrawPropertyRow(ref rect, boundsOverridesProp, "Smart Bounds Overrides");

//             EditorGUI.indentLevel--;

//             EditorGUI.EndProperty();
//         }

//         private static GUIContent GetFoldoutLabel(SerializedProperty property, GUIContent incomingLabel)
//         {
//             if (incomingLabel != null && !string.IsNullOrWhiteSpace(incomingLabel.text))
//                 return incomingLabel;

//             return new GUIContent(ObjectNames.NicifyVariableName(property.name));
//         }

//         private static void DrawPropertyRow(ref Rect rect, SerializedProperty prop, string label)
//         {
//             float spacing = EditorGUIUtility.standardVerticalSpacing;
//             float height = EditorGUI.GetPropertyHeight(prop, includeChildren: true);

//             Rect row = EditorGUI.IndentedRect(new Rect(rect.x, rect.y, rect.width, height));
//             EditorGUI.PropertyField(row, prop, new GUIContent(label), includeChildren: true);

//             rect.y += height + spacing;
//         }

//         private static bool ShowsLabel(TargetIncludeMode includeMode)
//         {
//             switch (includeMode)
//             {
//                 case TargetIncludeMode.ImmediateChildrenWithLabel:
//                 case TargetIncludeMode.FirstMatchingDepthWithLabel:
//                 case TargetIncludeMode.AllMatchingDescendantsWithLabel:
//                     return true;

//                 case TargetIncludeMode.RootOnly:
//                 case TargetIncludeMode.ImmediateChildren:
//                 default:
//                     return false;
//             }
//         }

//         private static bool RequiresLabel(TargetIncludeMode includeMode)
//         {
//             switch (includeMode)
//             {
//                 case TargetIncludeMode.FirstMatchingDepthWithLabel:
//                 case TargetIncludeMode.AllMatchingDescendantsWithLabel:
//                     return true;

//                 default:
//                     return false;
//             }
//         }

//         private static float GetHelpBoxHeight(string message)
//         {
//             return EditorStyles.helpBox.CalcHeight(
//                 new GUIContent(message),
//                 EditorGUIUtility.currentViewWidth - 60f);
//         }
//     }
// }