using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    public static class ShapeStamperBakeService
    {
        public static void Bake(IReadOnlyList<Vector2> shapePoints, IReadOnlyList<Vector2> profilePoints)
        {
            var go = new GameObject("ShapeStamp_Baked");
            Undo.RegisterCreatedObjectUndo(go, "Bake Shape Stamp");

            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();

            var mesh = ShapeStamperMeshBuilder.BuildPreviewExtrude(shapePoints, profilePoints);

            mf.sharedMesh = mesh;

            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
        }
    }
}