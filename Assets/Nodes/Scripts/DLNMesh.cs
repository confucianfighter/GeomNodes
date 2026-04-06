using UnityEngine;
using CodeSmile.GraphMesh;
using Unity.Mathematics;
using System.Collections.Generic;
using System.Linq;
using CodeSmile.GraphMesh;
using Unity.Collections;



namespace DLN
{
    public static class DLNMesh
    {
        public static void CreateTriangle(Vector3 baseTangent, Vector3 baseNorm, float baseLength, float altitude, out GMesh gMesh)
        {
            gMesh = new GMesh();
        }

        public static void ToMesh(GMesh mesh, out Mesh unityMesh, bool createGameObject = false)
        {
            unityMesh = mesh.ToMesh();
            if (createGameObject)
            {
                var go = new GameObject();
                var mf = go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                mf.mesh = unityMesh;
            }
        }

        public static void ToRenderer(GMesh mesh, Material mat, out GameObject GameObject_)
        {
            GameObject_ = new GameObject();
            ToMesh(mesh: mesh, unityMesh: out var graphics );
            var mf = GameObject_.AddComponent<MeshFilter>();
            mf.sharedMesh = graphics;
            GameObject_.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        public static void ToExtendedObject(GMesh graphicsMesh, GMesh colliderMesh, out GameObject ExtendedObject_)
        {
            ExtendedObject_ = new GameObject();
            ToMesh(mesh: graphicsMesh, unityMesh: out var graphics );
            ToMesh(mesh: colliderMesh, out var collider);
            var mf = ExtendedObject_.AddComponent<MeshFilter>();
            mf.sharedMesh = graphics;
            ExtendedObject_.AddComponent<MeshRenderer>();
            var col = ExtendedObject_.AddComponent<MeshCollider>();
            col.sharedMesh = collider;
        }

        public static void CreateShape(IEnumerable<Vector2> points, out GMesh gMesh_)
        {
            gMesh_ = new GMesh();
            var verts = new NativeArray<float3>(points.Count(), Allocator.Temp);
            foreach (var (pt, idx) in points.Select((pt, idx) => (pt, idx)))
            {
                verts[idx] = new float3(pt.x, pt.y, 0f);
            }
            gMesh_.CreateFace(verts);       // triangulates and returns a Unity Mesh
            verts.Dispose();
        }
        
        
    }
}