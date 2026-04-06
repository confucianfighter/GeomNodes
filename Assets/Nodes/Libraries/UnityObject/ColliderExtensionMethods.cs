using UnityEngine;
using System;

namespace DLN
{
    public static class CapsuleColliderExtentions
    {
        public static void Update(this CapsuleCollider col, CapsuleData data)
        {
            data.UpdateCollider(col);
        }
        public static void Update(this SphereCollider col, SphereData data)
        {
            data.UpdateCollider(col);
        }

    }
}