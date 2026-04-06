// #if UNITY_EDITOR
// using UnityEditor;
// using UnityEngine;

// namespace DLN.EditorTools
// {
//     [CustomEditor(typeof(QuickInvoke))]
//     public class QuickInvokeEditor : UnityEditor.Editor
//     {
//         public override void OnInspectorGUI()
//         {
//             DrawDefaultInspector();

//             EditorGUILayout.Space(8);

//             using (new EditorGUILayout.HorizontalScope())
//             {
//                 GUILayout.FlexibleSpace();

//                 if (GUILayout.Button("Invoke Event ▶", GUILayout.Width(160), GUILayout.Height(28)))
//                 {
//                     var t = (QuickInvoke)target;

//                     // Makes sure Undo/redo + prefab overrides behave sensibly
//                     Undo.RecordObject(t, "QuickInvoke Invoke");

//                     // Optional: mark dirty so prefab override records if any serialized state changes elsewhere
//                     // (UnityEvent invocation itself doesn't change this object, but left harmless)
//                     EditorUtility.SetDirty(t);

//                     t.InvokeNow();
//                 }

//                 GUILayout.FlexibleSpace();
//             }
//         }
//     }
// }
// #endif
