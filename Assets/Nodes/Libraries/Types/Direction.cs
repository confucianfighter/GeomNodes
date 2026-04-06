using UnityEngine;
using System;
using System.Collections.Generic;

namespace DLN
{

    /// <summary>
    /// Represents the six cardinal directions in 3D space.
    /// </summary>
    /// <remarks>
    /// This enum is used to define directions in a 3D space, such as in Unity.
    /// </remarks>

    public enum Direction
    {
        Forward,
        Back,
        Left,
        Right,
        Up,
        Down
    }

    public static class DirectionExtensions
    {
        public static Vector3 ToVector3(this Direction dir)
        {
            switch (dir)
            {
                case Direction.Forward: return Vector3.forward;
                case Direction.Back: return Vector3.back;
                case Direction.Left: return Vector3.left;
                case Direction.Right: return Vector3.right;
                case Direction.Up: return Vector3.up;
                case Direction.Down: return Vector3.down;
                default: throw new ArgumentOutOfRangeException(nameof(dir), dir, null);
            }
        }
    }
}
