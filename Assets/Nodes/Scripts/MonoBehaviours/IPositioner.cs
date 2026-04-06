using UnityEngine;
using System;

namespace DLN
{
    public interface IPositioner
    {
        void Position(Transform thingToPosition, Transform target);
    }
}