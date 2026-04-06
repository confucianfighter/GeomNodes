// using UnityEditor;
// using UnityEngine;

// namespace DLN.Editor
// {
//     [CustomPropertyDrawer(typeof(ResizeSettings))]
//     public class ResizeSettingsDrawer : PropertyDrawer
//     {
//         private const float DividerThickness = 1f;
//         private const float DividerPadding = 4f;

//         public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
//         {
//             float line = EditorGUIUtility.singleLineHeight;
//             float spacing = EditorGUIUtility.standardVerticalSpacing;
//             float height = 0f;

//             SerializedProperty resizeTargetsProp = property.FindPropertyRelative("resizeTargets");
//             SerializedProperty measureTargetsProp = property.FindPropertyRelative("measureTargets");
//             SerializedProperty pivotProp = property.FindPropertyRelative("pivot");
//             SerializedProperty resizeKindProp = property.FindPropertyRelative("resizeKind");

//             height += line + spacing; // title

//             height += EditorGUI.GetPropertyHeight(resizeTargetsProp, true) + spacing;

//             if (ShouldShowMeasureTargets(property))
//                 height += EditorGUI.GetPropertyHeight(measureTargetsProp, true) + spacing;

//             height += EditorGUI.GetPropertyHeight(pivotProp, true) + spacing;
//             height += EditorGUI.GetPropertyHeight(resizeKindProp, true) + spacing;

//             height += DividerPadding + DividerThickness + DividerPadding + spacing;

//             ResizeKind resizeKind = (ResizeKind)resizeKindProp.enumValueIndex;
//             if (resizeKind == ResizeKind.Uniform)
//                 height += GetUniformHeight(property);
//             else
//                 height += GetNonUniformHeight(property);

//             return height;
//         }

//         public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
//         {
//             EditorGUI.BeginProperty(position, label, property);

//             float spacing = EditorGUIUtility.standardVerticalSpacing;

//             SerializedProperty resizeTargetsProp = property.FindPropertyRelative("resizeTargets");
//             SerializedProperty measureTargetsProp = property.FindPropertyRelative("measureTargets");
//             SerializedProperty pivotProp = property.FindPropertyRelative("pivot");
//             SerializedProperty resizeKindProp = property.FindPropertyRelative("resizeKind");

//             Rect rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

//             //EditorGUI.LabelField(rect, label, EditorStyles.boldLabel);
//             rect.y += rect.height + spacing;

//             DrawPropertyRow(ref rect, resizeTargetsProp, "Item(s) To Resize");

//             if (ShouldShowMeasureTargets(property))
//                 DrawPropertyRow(ref rect, measureTargetsProp, "Item(s) To Fit To");

//             DrawPropertyRow(ref rect, pivotProp, "Pivot");
//             DrawPropertyRow(ref rect, resizeKindProp, "Resize Kind");

//             rect.y += DividerPadding;
//             DrawDivider(new Rect(rect.x, rect.y, rect.width, DividerThickness));
//             rect.y += DividerThickness + DividerPadding + spacing;

//             ResizeKind resizeKind = (ResizeKind)resizeKindProp.enumValueIndex;
//             if (resizeKind == ResizeKind.Uniform)
//                 DrawUniformSettings(ref rect, property);
//             else
//                 DrawNonUniformSettings(ref rect, property);

//             EditorGUI.EndProperty();
//         }

//         private float GetUniformHeight(SerializedProperty property)
//         {
//             float line = EditorGUIUtility.singleLineHeight;
//             float spacing = EditorGUIUtility.standardVerticalSpacing;
//             float height = 0f;

//             SerializedProperty uniformProp = property.FindPropertyRelative("uniform");

//             SerializedProperty fitTypeProp = uniformProp.FindPropertyRelative("fitType");

//             SerializedProperty fitXSourceProp = uniformProp.FindPropertyRelative("fitXSource");
//             SerializedProperty fitYSourceProp = uniformProp.FindPropertyRelative("fitYSource");
//             SerializedProperty fitZSourceProp = uniformProp.FindPropertyRelative("fitZSource");
//             SerializedProperty fixedFitSizesProp = uniformProp.FindPropertyRelative("fixedFitSizes");

//             SerializedProperty outputXModeProp = uniformProp.FindPropertyRelative("outputXMode");
//             SerializedProperty outputYModeProp = uniformProp.FindPropertyRelative("outputYMode");
//             SerializedProperty outputZModeProp = uniformProp.FindPropertyRelative("outputZMode");
//             SerializedProperty fixedOutputSizesProp = uniformProp.FindPropertyRelative("fixedOutputSizes");

//             SerializedProperty resizeMethodProp = uniformProp.FindPropertyRelative("resizeMethod");
//             SerializedProperty scaleIndependentProp = uniformProp.FindPropertyRelative("scaleIndependentOfChildren");

//             height += line + spacing; // Fit header
//             height += EditorGUI.GetPropertyHeight(fitTypeProp, true) + spacing;

//             height += line + spacing; // Fit Axis Sources header
//             height += EditorGUI.GetPropertyHeight(fitXSourceProp, true) + spacing;
//             height += EditorGUI.GetPropertyHeight(fitYSourceProp, true) + spacing;
//             height += EditorGUI.GetPropertyHeight(fitZSourceProp, true) + spacing;

//             if (IsUniformFitFixed(fitXSourceProp) || IsUniformFitFixed(fitYSourceProp) || IsUniformFitFixed(fitZSourceProp))
//             {
//                 height += line + spacing; // Fixed Fit Sizes header

//                 if (IsUniformFitFixed(fitXSourceProp))
//                     height += GetAxisChildHeight(fixedFitSizesProp, "X") + spacing;

//                 if (IsUniformFitFixed(fitYSourceProp))
//                     height += GetAxisChildHeight(fixedFitSizesProp, "Y") + spacing;

//                 if (IsUniformFitFixed(fitZSourceProp))
//                     height += GetAxisChildHeight(fixedFitSizesProp, "Z") + spacing;
//             }

//             height += line + spacing; // Output Axis Modes header
//             height += EditorGUI.GetPropertyHeight(outputXModeProp, true) + spacing;
//             height += EditorGUI.GetPropertyHeight(outputYModeProp, true) + spacing;
//             height += EditorGUI.GetPropertyHeight(outputZModeProp, true) + spacing;

//             if (IsUniformOutputFixed(outputXModeProp) || IsUniformOutputFixed(outputYModeProp) || IsUniformOutputFixed(outputZModeProp))
//             {
//                 height += line + spacing; // Fixed Output Sizes header

//                 if (IsUniformOutputFixed(outputXModeProp))
//                     height += GetAxisChildHeight(fixedOutputSizesProp, "X") + spacing;

//                 if (IsUniformOutputFixed(outputYModeProp))
//                     height += GetAxisChildHeight(fixedOutputSizesProp, "Y") + spacing;

//                 if (IsUniformOutputFixed(outputZModeProp))
//                     height += GetAxisChildHeight(fixedOutputSizesProp, "Z") + spacing;
//             }

//             height += line + spacing; // Application header
//             height += EditorGUI.GetPropertyHeight(resizeMethodProp, true) + spacing;

//             if (IsScaleTransform(resizeMethodProp))
//                 height += EditorGUI.GetPropertyHeight(scaleIndependentProp, true) + spacing;

//             return height;
//         }

//         private float GetNonUniformHeight(SerializedProperty property)
//         {
//             float line = EditorGUIUtility.singleLineHeight;
//             float spacing = EditorGUIUtility.standardVerticalSpacing;
//             float height = 0f;

//             SerializedProperty nonUniformProp = property.FindPropertyRelative("nonUniform");

//             SerializedProperty xModeProp = nonUniformProp.FindPropertyRelative("xMode");
//             SerializedProperty yModeProp = nonUniformProp.FindPropertyRelative("yMode");
//             SerializedProperty zModeProp = nonUniformProp.FindPropertyRelative("zMode");
//             SerializedProperty fixedSizesProp = nonUniformProp.FindPropertyRelative("fixedSizes");
//             SerializedProperty resizeMethodProp = nonUniformProp.FindPropertyRelative("resizeMethod");
//             SerializedProperty cornerRegionPercentProp = nonUniformProp.FindPropertyRelative("cornerRegionPercent");

//             height += line + spacing; // Axis Modes header
//             height += EditorGUI.GetPropertyHeight(xModeProp, true) + spacing;
//             height += EditorGUI.GetPropertyHeight(yModeProp, true) + spacing;
//             height += EditorGUI.GetPropertyHeight(zModeProp, true) + spacing;

//             if (IsNonUniformFixed(xModeProp) || IsNonUniformFixed(yModeProp) || IsNonUniformFixed(zModeProp))
//             {
//                 height += line + spacing; // Fixed Sizes header

//                 if (IsNonUniformFixed(xModeProp))
//                     height += GetAxisChildHeight(fixedSizesProp, "X") + spacing;

//                 if (IsNonUniformFixed(yModeProp))
//                     height += GetAxisChildHeight(fixedSizesProp, "Y") + spacing;

//                 if (IsNonUniformFixed(zModeProp))
//                     height += GetAxisChildHeight(fixedSizesProp, "Z") + spacing;
//             }

//             height += line + spacing; // Deformation header
//             height += EditorGUI.GetPropertyHeight(resizeMethodProp, true) + spacing;

//             if (IsCornerPreserving(resizeMethodProp))
//                 height += EditorGUI.GetPropertyHeight(cornerRegionPercentProp, true) + spacing;

//             return height;
//         }

//         private void DrawUniformSettings(ref Rect rect, SerializedProperty property)
//         {
//             SerializedProperty uniformProp = property.FindPropertyRelative("uniform");

//             SerializedProperty fitTypeProp = uniformProp.FindPropertyRelative("fitType");

//             SerializedProperty fitXSourceProp = uniformProp.FindPropertyRelative("fitXSource");
//             SerializedProperty fitYSourceProp = uniformProp.FindPropertyRelative("fitYSource");
//             SerializedProperty fitZSourceProp = uniformProp.FindPropertyRelative("fitZSource");
//             SerializedProperty fixedFitSizesProp = uniformProp.FindPropertyRelative("fixedFitSizes");

//             SerializedProperty outputXModeProp = uniformProp.FindPropertyRelative("outputXMode");
//             SerializedProperty outputYModeProp = uniformProp.FindPropertyRelative("outputYMode");
//             SerializedProperty outputZModeProp = uniformProp.FindPropertyRelative("outputZMode");
//             SerializedProperty fixedOutputSizesProp = uniformProp.FindPropertyRelative("fixedOutputSizes");

//             SerializedProperty resizeMethodProp = uniformProp.FindPropertyRelative("resizeMethod");
//             SerializedProperty scaleIndependentProp = uniformProp.FindPropertyRelative("scaleIndependentOfChildren");

//             DrawHeaderRow(ref rect, "Fit");
//             DrawPropertyRow(ref rect, fitTypeProp, "Fit Type");

//             DrawHeaderRow(ref rect, "Fit Axis Sources");
//             DrawPropertyRow(ref rect, fitXSourceProp, "X Source");
//             DrawPropertyRow(ref rect, fitYSourceProp, "Y Source");
//             DrawPropertyRow(ref rect, fitZSourceProp, "Z Source");

//             if (IsUniformFitFixed(fitXSourceProp) || IsUniformFitFixed(fitYSourceProp) || IsUniformFitFixed(fitZSourceProp))
//             {
//                 DrawHeaderRow(ref rect, "Fixed Fit Sizes");

//                 if (IsUniformFitFixed(fitXSourceProp))
//                     DrawAxisChildRow(ref rect, fixedFitSizesProp, "X", "Fixed X Size");

//                 if (IsUniformFitFixed(fitYSourceProp))
//                     DrawAxisChildRow(ref rect, fixedFitSizesProp, "Y", "Fixed Y Size");

//                 if (IsUniformFitFixed(fitZSourceProp))
//                     DrawAxisChildRow(ref rect, fixedFitSizesProp, "Z", "Fixed Z Size");
//             }

//             DrawHeaderRow(ref rect, "Output Axis Modes");
//             DrawPropertyRow(ref rect, outputXModeProp, "X Output");
//             DrawPropertyRow(ref rect, outputYModeProp, "Y Output");
//             DrawPropertyRow(ref rect, outputZModeProp, "Z Output");

//             if (IsUniformOutputFixed(outputXModeProp) || IsUniformOutputFixed(outputYModeProp) || IsUniformOutputFixed(outputZModeProp))
//             {
//                 DrawHeaderRow(ref rect, "Fixed Output Sizes");

//                 if (IsUniformOutputFixed(outputXModeProp))
//                     DrawAxisChildRow(ref rect, fixedOutputSizesProp, "X", "Fixed X Size");

//                 if (IsUniformOutputFixed(outputYModeProp))
//                     DrawAxisChildRow(ref rect, fixedOutputSizesProp, "Y", "Fixed Y Size");

//                 if (IsUniformOutputFixed(outputZModeProp))
//                     DrawAxisChildRow(ref rect, fixedOutputSizesProp, "Z", "Fixed Z Size");
//             }

//             DrawHeaderRow(ref rect, "Application");
//             DrawPropertyRow(ref rect, resizeMethodProp, "Resize Method");

//             if (IsScaleTransform(resizeMethodProp))
//                 DrawPropertyRow(ref rect, scaleIndependentProp, "Scale Independent Of Children");
//         }

//         private void DrawNonUniformSettings(ref Rect rect, SerializedProperty property)
//         {
//             SerializedProperty nonUniformProp = property.FindPropertyRelative("nonUniform");

//             SerializedProperty xModeProp = nonUniformProp.FindPropertyRelative("xMode");
//             SerializedProperty yModeProp = nonUniformProp.FindPropertyRelative("yMode");
//             SerializedProperty zModeProp = nonUniformProp.FindPropertyRelative("zMode");
//             SerializedProperty fixedSizesProp = nonUniformProp.FindPropertyRelative("fixedSizes");
//             SerializedProperty resizeMethodProp = nonUniformProp.FindPropertyRelative("resizeMethod");
//             SerializedProperty cornerRegionPercentProp = nonUniformProp.FindPropertyRelative("cornerRegionPercent");

//             DrawHeaderRow(ref rect, "Axis Modes");
//             DrawPropertyRow(ref rect, xModeProp, "X Mode");
//             DrawPropertyRow(ref rect, yModeProp, "Y Mode");
//             DrawPropertyRow(ref rect, zModeProp, "Z Mode");

//             if (IsNonUniformFixed(xModeProp) || IsNonUniformFixed(yModeProp) || IsNonUniformFixed(zModeProp))
//             {
//                 DrawHeaderRow(ref rect, "Fixed Sizes");

//                 if (IsNonUniformFixed(xModeProp))
//                     DrawAxisChildRow(ref rect, fixedSizesProp, "X", "Fixed X Size");

//                 if (IsNonUniformFixed(yModeProp))
//                     DrawAxisChildRow(ref rect, fixedSizesProp, "Y", "Fixed Y Size");

//                 if (IsNonUniformFixed(zModeProp))
//                     DrawAxisChildRow(ref rect, fixedSizesProp, "Z", "Fixed Z Size");
//             }

//             DrawHeaderRow(ref rect, "Deformation");
//             DrawPropertyRow(ref rect, resizeMethodProp, "Resize Method");

//             if (IsCornerPreserving(resizeMethodProp))
//                 DrawPropertyRow(ref rect, cornerRegionPercentProp, "Corner Region Percent");
//         }

//         private static void DrawPropertyRow(ref Rect rect, SerializedProperty prop, string label)
//         {
//             float spacing = EditorGUIUtility.standardVerticalSpacing;
//             float height = EditorGUI.GetPropertyHeight(prop, true);

//             Rect row = new Rect(rect.x, rect.y, rect.width, height);
//             EditorGUI.PropertyField(row, prop, new GUIContent(label), true);

//             rect.y += height + spacing;
//         }

//         private static void DrawAxisChildRow(ref Rect rect, SerializedProperty parentProp, string childName, string label)
//         {
//             float spacing = EditorGUIUtility.standardVerticalSpacing;
//             SerializedProperty childProp = parentProp.FindPropertyRelative(childName);
//             float height = EditorGUI.GetPropertyHeight(childProp, true);

//             Rect row = new Rect(rect.x, rect.y, rect.width, height);
//             EditorGUI.PropertyField(row, childProp, new GUIContent(label), true);

//             rect.y += height + spacing;
//         }

//         private static void DrawHeaderRow(ref Rect rect, string text)
//         {
//             float line = EditorGUIUtility.singleLineHeight;
//             float spacing = EditorGUIUtility.standardVerticalSpacing;

//             Rect row = new Rect(rect.x, rect.y, rect.width, line);
//             EditorGUI.LabelField(row, text, EditorStyles.boldLabel);

//             rect.y += line + spacing;
//         }

//         private static float GetAxisChildHeight(SerializedProperty parentProp, string childName)
//         {
//             SerializedProperty childProp = parentProp.FindPropertyRelative(childName);
//             return EditorGUI.GetPropertyHeight(childProp, true);
//         }

//         private bool ShouldShowMeasureTargets(SerializedProperty property)
//         {
//             SerializedProperty resizeKindProp = property.FindPropertyRelative("resizeKind");
//             ResizeKind resizeKind = (ResizeKind)resizeKindProp.enumValueIndex;

//             if (resizeKind == ResizeKind.Uniform)
//             {
//                 SerializedProperty uniformProp = property.FindPropertyRelative("uniform");
//                 SerializedProperty fitX = uniformProp.FindPropertyRelative("fitXSource");
//                 SerializedProperty fitY = uniformProp.FindPropertyRelative("fitYSource");
//                 SerializedProperty fitZ = uniformProp.FindPropertyRelative("fitZSource");

//                 bool allFixedOrIgnored =
//                     IsUniformFitNotTarget(fitX) &&
//                     IsUniformFitNotTarget(fitY) &&
//                     IsUniformFitNotTarget(fitZ);

//                 return !allFixedOrIgnored;
//             }

//             SerializedProperty nonUniformProp = property.FindPropertyRelative("nonUniform");
//             SerializedProperty xMode = nonUniformProp.FindPropertyRelative("xMode");
//             SerializedProperty yMode = nonUniformProp.FindPropertyRelative("yMode");
//             SerializedProperty zMode = nonUniformProp.FindPropertyRelative("zMode");

//             bool allFixedOrPreserve =
//                 IsNonUniformNotTarget(xMode) &&
//                 IsNonUniformNotTarget(yMode) &&
//                 IsNonUniformNotTarget(zMode);

//             return !allFixedOrPreserve;
//         }

//         private bool IsUniformFitFixed(SerializedProperty prop)
//         {
//             return (UniformFitAxisSource)prop.enumValueIndex == UniformFitAxisSource.Fixed;
//         }

//         private bool IsUniformFitNotTarget(SerializedProperty prop)
//         {
//             return (UniformFitAxisSource)prop.enumValueIndex != UniformFitAxisSource.Target;
//         }

//         private bool IsUniformOutputFixed(SerializedProperty prop)
//         {
//             return (UniformOutputAxisMode)prop.enumValueIndex == UniformOutputAxisMode.Fixed;
//         }

//         private bool IsNonUniformFixed(SerializedProperty prop)
//         {
//             return (NonUniformAxisMode)prop.enumValueIndex == NonUniformAxisMode.Fixed;
//         }

//         private bool IsNonUniformNotTarget(SerializedProperty prop)
//         {
//             return (NonUniformAxisMode)prop.enumValueIndex != NonUniformAxisMode.Target;
//         }

//         private bool IsScaleTransform(SerializedProperty prop)
//         {
//             return (ResizeMethod)prop.enumValueIndex == ResizeMethod.ScaleTransform;
//         }

//         private bool IsCornerPreserving(SerializedProperty prop)
//         {
//             return (NonUniformResizeMethod)prop.enumValueIndex == NonUniformResizeMethod.CornerPreserving;
//         }

//         private static void DrawDivider(Rect rect)
//         {
//             EditorGUI.DrawRect(rect, new Color(0.35f, 0.35f, 0.35f, 1f));
//         }
//     }
// }