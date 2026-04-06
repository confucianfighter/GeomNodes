using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using PBEdge = UnityEngine.ProBuilder.Edge;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DLN
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ProBuilderMesh), typeof(MeshFilter), typeof(MeshRenderer))]
    public class PB_BevelAll : MonoBehaviour
    {
        [Header("Shape")]
        public bool generateCubeIfEmpty = true;
        public Vector3 cubeSize = Vector3.one;

        [Header("Bevel")]
        [Range(0f, 1f)]
        public float bevelAmount = 0.08f;

        [Header("Materials")]
        public Material baseMaterial;
        public Material bevelMaterial;

        [Header("Editor")]
        public bool rebuildOnValidate = true;

        private ProBuilderMesh _pb;
        private MeshRenderer _mr;

        private void Reset()
        {
            EnsureRefs();
            Rebuild();
        }

        private void OnValidate()
        {
            if (!rebuildOnValidate)
                return;

            EnsureRefs();
            Rebuild();
        }

        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            EnsureRefs();
            if (_pb == null || _mr == null)
                return;

            if (generateCubeIfEmpty && (_pb.faces == null || _pb.faces.Count == 0))
                RebuildAsCube();

            if (_pb.faces == null || _pb.faces.Count == 0)
                return;

            // Base material on all existing faces first.
            if (baseMaterial != null)
                _pb.SetMaterial(_pb.faces, baseMaterial);

            // Put the materials in the renderer slots up front.
            ApplyRendererMaterials();

            var allEdges = CollectAllDistinctEdges(_pb);
            if (allEdges.Count == 0 || bevelAmount <= 0f)
            {
                Commit();
                return;
            }

            // Bevel returns the newly-created bevel faces.
            List<Face> bevelFaces = Bevel.BevelEdges(_pb, allEdges, bevelAmount);

            if (bevelMaterial != null && bevelFaces != null && bevelFaces.Count > 0)
                _pb.SetMaterial(bevelFaces, bevelMaterial);

            Commit();
        }

        private void EnsureRefs()
        {
            if (_pb == null) _pb = GetComponent<ProBuilderMesh>();
            if (_mr == null) _mr = GetComponent<MeshRenderer>();
        }

        private void RebuildAsCube()
        {
            ProBuilderMesh temp = ShapeGenerator.CreateShape(ShapeType.Cube);
            temp.transform.localScale = cubeSize;

            temp.ToMesh();
            temp.Refresh();

            _pb.CopyFrom(temp);

#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(temp.gameObject);
            else
                Destroy(temp.gameObject);
#else
            Destroy(temp.gameObject);
#endif
        }

        private void ApplyRendererMaterials()
        {
            if (baseMaterial != null && bevelMaterial != null)
                _mr.sharedMaterials = new[] { baseMaterial, bevelMaterial };
            else if (baseMaterial != null)
                _mr.sharedMaterials = new[] { baseMaterial };
            else if (bevelMaterial != null)
                _mr.sharedMaterials = new[] { bevelMaterial };
        }

        private void Commit()
        {
            _pb.ToMesh();
            _pb.Refresh();
            ApplyRendererMaterials();
        }

        private static List<PBEdge> CollectAllDistinctEdges(ProBuilderMesh pb)
        {
            var result = new List<PBEdge>();
            var seen = new HashSet<(int, int)>();

            foreach (var face in pb.faces)
            {
                foreach (var edge in face.edges)
                {
                    int a = Mathf.Min(edge.a, edge.b);
                    int b = Mathf.Max(edge.a, edge.b);

                    if (seen.Add((a, b)))
                        result.Add(new PBEdge(a, b));
                }
            }

            return result;
        }
    }
}