using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using DLN.EditorTools.ShapeStamper;
#endif

namespace DLN
{
    [System.Serializable]
    public sealed class AdaptiveShapeBuildResult
    {
#if UNITY_EDITOR
        public ShapeStamperProfileGenerator.ShapeStampRingBuildResult RingBuildResult;
#endif

        public List<Mesh> SegmentMeshes = new();
        public Mesh StartCapMesh;
        public Mesh EndCapMesh;

        public int SegmentCount => SegmentMeshes != null ? SegmentMeshes.Count : 0;

        public bool HasCaps => StartCapMesh != null || EndCapMesh != null;

        public void Clear()
        {
#if UNITY_EDITOR
            RingBuildResult = null;
#endif
            SegmentMeshes.Clear();
            StartCapMesh = null;
            EndCapMesh = null;
        }
    }
}