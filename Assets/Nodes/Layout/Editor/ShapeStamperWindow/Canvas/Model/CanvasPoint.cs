using System;
using UnityEngine;

namespace DLN
{
    [Serializable]
    public struct CanvasPoint
    {
        public int Id;
        public Vector2 Position;

        public CanvasAnchorX XAnchor;
        public CanvasAnchorY YAnchor;

        public float OffsetX;
        public float OffsetY;

        public CanvasPoint(int id, Vector2 position)
        {
            Id = id;
            Position = position;

            XAnchor = CanvasAnchorX.Floating;
            YAnchor = CanvasAnchorY.Floating;

            OffsetX = 0f;
            OffsetY = 0f;
        }
    }
}
