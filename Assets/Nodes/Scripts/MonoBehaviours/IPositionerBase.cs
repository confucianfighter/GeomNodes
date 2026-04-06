using UnityEngine;
using System;

namespace DLN
{
    public abstract class IPositionerBase : MonoBehaviour
    {
        public abstract void Position(Transform transformToPosition, Transform target);
    }
}