using UnityEditor;
using UnityEngine;

namespace DLN.Editor
{
    [CustomEditor(typeof(AdaptiveShape))]
    public sealed class AdaptiveShapeEditor : UnityEditor.Editor
    {
        private const string InspectorContext = "DLN.AdaptiveShapeInspector";

        private SerializedProperty smartBoundsProp;
        private SerializedProperty preferSmartBoundsBordersPaddingProp;
        private SerializedProperty fallbackBordersPaddingProp;
        private SerializedProperty sizeSettingsProp;
        private SerializedProperty materialSlotsProp;
        private SerializedProperty profileProp;
        private SerializedProperty profileEdgesProp;

        private void OnEnable()
        {
            smartBoundsProp = serializedObject.FindProperty("smartBounds");
            preferSmartBoundsBordersPaddingProp = serializedObject.FindProperty("preferSmartBoundsBordersPadding");
            fallbackBordersPaddingProp = serializedObject.FindProperty("fallbackBordersPadding");
            sizeSettingsProp = serializedObject.FindProperty("sizeSettings");
            materialSlotsProp = serializedObject.FindProperty("materialSlots");
            profileProp = serializedObject.FindProperty("profile");
            profileEdgesProp = profileProp.FindPropertyRelative("edges");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawBoundsSection();
            EditorGUILayout.Space();
            DrawSizeSection();
            EditorGUILayout.Space();
            DrawMaterialSlotsSection();
            EditorGUILayout.Space();
            DrawProfileSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawBoundsSection()
        {
            EditorGUILayout.LabelField("Bounds Source", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(smartBoundsProp);
            EditorGUILayout.PropertyField(preferSmartBoundsBordersPaddingProp);
            EditorGUILayout.PropertyField(fallbackBordersPaddingProp);
        }

        private void DrawSizeSection()
        {
            EditorGUILayout.LabelField("Size", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(sizeSettingsProp, true);

            if (targets.Length == 1 && target is AdaptiveShape adaptiveShape)
            {
                Vector2 innerSize = adaptiveShape.GetCurrentInnerSize();
                EditorGUILayout.HelpBox(
                    $"Current inner size (X,Y): {innerSize.x:0.###}, {innerSize.y:0.###}",
                    MessageType.None);
            }

            EditorGUILayout.HelpBox(
                "Current bridge behavior: the minContentsSize values are interpreted as inner-size distances between the two padding edges.",
                MessageType.Info);
        }

        private void DrawMaterialSlotsSection()
        {
            EditorGUILayout.LabelField("Material Slots", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(materialSlotsProp, true);

            if (materialSlotsProp.arraySize == 0)
                EditorGUILayout.HelpBox("Add at least one slot before assigning profile edges.", MessageType.Warning);
        }

        private void DrawProfileSection()
        {
            EditorGUILayout.LabelField("Profile", EditorStyles.boldLabel);

            bool expanded = PropertySessionState.GetBool(profileProp, InspectorContext, "ProfileEdgesExpanded", true);
            expanded = EditorGUILayout.Foldout(expanded, "Edges", true);
            PropertySessionState.SetBool(profileProp, InspectorContext, "ProfileEdgesExpanded", expanded);

            if (!expanded)
                return;

            EditorGUI.indentLevel++;

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add Edge", GUILayout.Width(100f)))
            {
                int index = profileEdgesProp.arraySize;
                profileEdgesProp.InsertArrayElementAtIndex(index);
                SerializedProperty edge = profileEdgesProp.GetArrayElementAtIndex(index);
                edge.FindPropertyRelative("name").stringValue = $"Edge {index}";
                edge.FindPropertyRelative("materialSlotIndex").intValue = 0;
            }
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < profileEdgesProp.arraySize; i++)
            {
                DrawEdgeRow(i);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawEdgeRow(int index)
        {
            SerializedProperty edge = profileEdgesProp.GetArrayElementAtIndex(index);
            SerializedProperty edgeName = edge.FindPropertyRelative("name");
            SerializedProperty slotIndex = edge.FindPropertyRelative("materialSlotIndex");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            edgeName.stringValue = EditorGUILayout.TextField("Name", edgeName.stringValue);
            if (GUILayout.Button("X", GUILayout.Width(24f)))
            {
                profileEdgesProp.DeleteArrayElementAtIndex(index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            EditorGUILayout.EndHorizontal();

            if (materialSlotsProp.arraySize > 0)
            {
                string[] slotNames = BuildSlotNames();
                int safeIndex = Mathf.Clamp(slotIndex.intValue, 0, materialSlotsProp.arraySize - 1);
                int next = EditorGUILayout.Popup("Material Slot", safeIndex, slotNames);
                slotIndex.intValue = next;
            }
            else
            {
                EditorGUILayout.HelpBox("No material slots available.", MessageType.Warning);
                slotIndex.intValue = 0;
            }

            EditorGUILayout.EndVertical();
        }

        private string[] BuildSlotNames()
        {
            string[] names = new string[materialSlotsProp.arraySize];

            for (int i = 0; i < materialSlotsProp.arraySize; i++)
            {
                SerializedProperty slot = materialSlotsProp.GetArrayElementAtIndex(i);
                string label = slot.FindPropertyRelative("name").stringValue;
                SerializedProperty kindProp = slot.FindPropertyRelative("kind");
                string kind = kindProp.enumDisplayNames[kindProp.enumValueIndex];

                if (string.IsNullOrWhiteSpace(label))
                    label = $"Slot {i}";

                names[i] = $"{i}: {label} ({kind})";
            }

            return names;
        }
    }
}
