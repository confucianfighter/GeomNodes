using UnityEngine;

namespace DLN
{
    public class C3DLS_BakeScaleIntoMesh : LayoutOp
    {
        public Transform target;

        [ContextMenu("bake()")]
        public override void Execute()
        {
            if (target == null)
                target = transform;

            if (target == null)
            {
                Debug.LogError($"Missing target in {name} => {gameObject.name}");
                return;
            }

            MeshScaleBaker.BakeLocalScaleIntoMesh(
                root: target.gameObject,
                includeChildren: false,
                updateMeshCollider: true,
                forceUniqueMeshInstance: true);
        }
    }
}