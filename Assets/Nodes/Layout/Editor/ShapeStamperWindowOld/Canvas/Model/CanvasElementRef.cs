#if false
using System;
using UnityEngine;

namespace DLN
{
    public enum CanvasElementType
    {
        None = 0,
        Point = 1,
        Edge = 2,
        Offset = 3
    }

    [Serializable]
    public struct CanvasElementRef : IEquatable<CanvasElementRef>
    {
        [SerializeField] private CanvasElementType type;
        [SerializeField] private int id;

        public CanvasElementType Type => type;
        public int Id => id;

        public bool IsValid => type != CanvasElementType.None && id >= 0;
        public bool IsPoint => type == CanvasElementType.Point;
        public bool IsEdge => type == CanvasElementType.Edge;
        public bool IsOffset => type == CanvasElementType.Offset;

        public static CanvasElementRef None => default;

        public CanvasElementRef(CanvasElementType type, int id)
        {
            this.type = type;
            this.id = id;
        }

        public static CanvasElementRef ForPoint(int id)
        {
            return new CanvasElementRef(CanvasElementType.Point, id);
        }

        public static CanvasElementRef ForEdge(int id)
        {
            return new CanvasElementRef(CanvasElementType.Edge, id);
        }

        public static CanvasElementRef ForOffset(int id)
        {
            return new CanvasElementRef(CanvasElementType.Offset, id);
        }

        public bool Equals(CanvasElementRef other)
        {
            return type == other.type && id == other.id;
        }

        public override bool Equals(object obj)
        {
            return obj is CanvasElementRef other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)type * 397) ^ id;
            }
        }

        public override string ToString()
        {
            return IsValid ? $"{type}({id})" : "None";
        }

        public static bool operator ==(CanvasElementRef left, CanvasElementRef right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CanvasElementRef left, CanvasElementRef right)
        {
            return !left.Equals(right);
        }
    }
}
#endif
