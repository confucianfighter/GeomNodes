using UnityEngine;
using System.Collections.Generic;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

namespace DLN
{
    public class EncapsulateWithSphere : MonoBehaviour
    {

        public Material material;
        public float transparency = 0.5f;
        public float percentBorder = 0.1f;
        public Color color = Color.orange;
        public bool createOnStart = false;
        public Transform lineEndPointTx;
        // add random seed


        public GameObject target;
        private float _radius;
        [SerializeField] private ProBuilderMesh pb; // Optional: assign a prefab with a ProBuilderMesh and material
        [ContextMenu("Encapsulate With Sphere")]

        public void Encapsulate()
        {
            // create probuilder sphere
            if (Bnds.GetWorldRadialBounds(target, out _radius, out var centroid))
            {
                // Create default ProBuilder sphere (icosphere) with pivot at center
                pb = ShapeGenerator.CreateShape(ShapeType.Sphere, PivotLocation.Center); // <- only these args
                pb.GetComponent<MeshRenderer>().sharedMaterial = material;
                pb.transform.SetPositionAndRotation(centroid, Quaternion.identity);

                // Convert centroid to world space if your bounds are local-to-this
                var radiusWithBorder = GetRadiusIncludingBorder();

                float diameter = radiusWithBorder * 2f;
                float scale = diameter * (1f + percentBorder);
                pb.transform.localScale = Vector3.one * scale;

                // Optional: vertex colors etc. (depends on your helpers)
                pb.transform.SetParent(transform,
                    worldPositionStays: true); // Keep world position when parenting
                pb.Refresh();
                // skip a frame
                lineEndPointTx.position = new Vector3(
                    centroid.x,
                    centroid.y + radiusWithBorder,
                    centroid.z
                );
                lineEndPointTx.right = Vector3.down;

            }
        }
        public Transform GetLineEndPointTransform()
        {
            return lineEndPointTx;
        }
        public void SetMaterial(Material material)
        {
            this.material = material;
        }
        public float GetRadiusIncludingBorder()
        {
            return _radius + percentBorder * _radius;
        }
        private void Start()
        {
            if (target == null)
            {
                Debug.LogError("EncapsulateWithSphere: No target assigned. Please assign a target GameObject to encapsulate.");
            }
            else if (createOnStart)
            {
                Rebuild();
            }
        }

        [ContextMenu("Update Color")]
        public void UpdateColor()
        {

            var pb = GetComponent<ProBuilderMesh>();
            if (pb != null)
            {
                var colorWithTransparency = new Color(color.r, color.g, color.b, transparency);
                pb.GetComponent<MeshRenderer>().sharedMaterial.color = colorWithTransparency;
                pb.Refresh();
            }
        }
        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            if (pb) DestroyImmediate(pb.gameObject);
            Encapsulate();
        }
        public void DestroySphere() { if (pb) DestroyImmediate(pb.gameObject); }
        public bool IsVisible()
        {
            return pb != null;
        }
        public void Toggle()
        {
            if (IsVisible())
            {
                DestroySphere();
            }
            else
            {
                Rebuild();
            }
        }

    }
}