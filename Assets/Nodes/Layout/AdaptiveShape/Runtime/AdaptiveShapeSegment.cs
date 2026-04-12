using UnityEngine;

namespace DLN
{
    [DisallowMultipleComponent]
    public class AdaptiveShapeSegment : MonoBehaviour
    {
        [SerializeField] private int segmentIndex = -1;
        [SerializeField] private GameObject profileRingObject;
        [SerializeField] private GameObject profileStartOuterRingObject;
        [SerializeField] private GameObject profileEndOuterRingObject;
        [SerializeField] private GameObject profileStartInnerRingObject;
        [SerializeField] private GameObject profileEndInnerRingObject;
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

        public GameObject ProfileStartOuterRingObject
        {
            get => profileStartOuterRingObject;
            set => profileStartOuterRingObject = value;
        }

        public GameObject ProfileEndOuterRingObject
        {
            get => profileEndOuterRingObject;
            set => profileEndOuterRingObject = value;
        }

        public GameObject ProfileStartInnerRingObject
        {
            get => profileStartInnerRingObject;
            set => profileStartInnerRingObject = value;
        }

        public GameObject ProfileEndInnerRingObject
        {
            get => profileEndInnerRingObject;
            set => profileEndInnerRingObject = value;
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