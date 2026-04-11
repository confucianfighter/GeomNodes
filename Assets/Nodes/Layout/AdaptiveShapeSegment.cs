using UnityEngine;

namespace DLN
{
    [DisallowMultipleComponent]
    public class AdaptiveShapeSegment : MonoBehaviour
    {
        [SerializeField] private int segmentIndex = -1;
        [SerializeField] private GameObject profileRingObject;
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;

        public int SegmentIndex
        {
            get => segmentIndex;
            set => segmentIndex = value;
        }

        public GameObject ProfileRingObject
        {
            get => profileRingObject;
            set => profileRingObject = value;
        }

        public MeshFilter MeshFilter
        {
            get
            {
                EnsureReferences();
                return meshFilter;
            }
            set => meshFilter = value;
        }

        public MeshRenderer MeshRenderer
        {
            get
            {
                EnsureReferences();
                return meshRenderer;
            }
            set => meshRenderer = value;
        }

        private void Reset()
        {
            EnsureReferences();
        }

        private void OnValidate()
        {
            EnsureReferences();
        }

        public void EnsureReferences()
        {
            if (meshFilter == null)
                TryGetComponent(out meshFilter);

            if (meshRenderer == null)
                TryGetComponent(out meshRenderer);
        }
    }
}