using UnityEngine;
namespace DLN
{
    [System.Serializable]
    public struct ResizeArgs
    {
        public ResizeMethod resizeMethod;
        public GameObject target;
        public Vector3 size;
        public Vector3 pivot;
    }
    public enum ResizeMethod
    {
        Mesh,
        Scale
    }
}