using UnityEngine;
[System.Serializable]
public struct SphereData
{
    public Vector3 center;
    public float radius;

    public void UpdateCollider(SphereCollider col)
    {
        col.radius = radius;
        col.center = center;
    }
    public Bounds ToBounds()
    {
        return new Bounds(center: center, size: Vector3.one.normalized * radius * 2);
    }
}

