using UnityEngine;
using System;
using System.Linq;
using DLN.Extensions;


namespace DLN
{
    public static class ColliderUpdater
    {
        public static void RefreshCollider(Collider col, Bounds bounds, Mesh runtimeMesh = null)
        {
            switch (col)
            {
                case MeshCollider mc:
                    mc.sharedMesh = null;
                    mc.sharedMesh = runtimeMesh;
                    break;

                case BoxCollider box:
                    box.center = bounds.center;
                    box.size = bounds.size;
                    break;

                case SphereCollider sphere:
                    sphere.Update(bounds.ToSphere());
                    break;

                case CapsuleCollider capsule:
                    capsule.Update(bounds.ToCapsule());
                    break;
            }
        }
    }
}