using UnityEngine;

namespace DLN
{
    [System.Serializable]
    public struct CapsuleData
    {
        public Vector3 center;
        public int direction; // 0=X, 1=Y, 2=Z
        public float radius;
        public float height;

        public void UpdateCollider(CapsuleCollider col)
        {
            col.center = center;
            col.direction = direction;
            col.radius = radius;
            col.height = height;
        }
        public Bounds ToBounds()
        {
            return new Bounds(center: center, size: Vector3.one.normalized * radius / 2);
        }
    }

}