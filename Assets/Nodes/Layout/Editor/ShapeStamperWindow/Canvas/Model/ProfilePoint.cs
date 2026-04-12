using System;
using UnityEngine;

namespace DLN
{
    [Serializable]
    public struct ProfilePoint
    {
        public int Id;
        public Vector2 Position;

        public CanvasAnchorY YAnchor;
        public float OffsetY;

        public ProfileXSpan XSpan;
        public ProfileZSpan ZSpan;
        public float XT;
        public float ZT;

        public ProfilePoint(int id, Vector2 position)
        {
            Id = id;
            Position = position;

            YAnchor = CanvasAnchorY.Floating;
            OffsetY = 0f;

            XSpan = ProfileXSpan.PaddingToContent;
            ZSpan = ProfileZSpan.MainDepth;
            XT = 0f;
            ZT = 0f;
        }
    }
}
