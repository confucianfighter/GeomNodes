using System;
using System.Collections.Generic;
using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    [Serializable]
    public class ProfileCanvasDocument : ICanvasDocument
    {
        [SerializeField] private List<CanvasPoint> points = new();
        [SerializeField] private List<CanvasEdge> edges = new();
        [SerializeField] private List<CanvasOffsetConstraint> offsets = new();

        public IList<CanvasPoint> Points => points;
        public IList<CanvasEdge> Edges => edges;
        public IList<CanvasOffsetConstraint> Offsets => offsets;

        public void MarkDirty()
        {
        }

        public void EnsureValidProfile()
        {
            if (points.Count == 0)
                ResetDefaultProfile();
        }

        public void ResetDefaultProfile()
        {
            points.Clear();
            edges.Clear();
            offsets.Clear();

            points.Add(new CanvasPoint { Id = 0, Position = new Vector2(0.1f, 0.1f) });
            points.Add(new CanvasPoint { Id = 1, Position = new Vector2(0.3f, 0.2f) });
            points.Add(new CanvasPoint { Id = 2, Position = new Vector2(0.5f, 0.5f) });

            edges.Add(new CanvasEdge { Id = 0, A = 0, B = 1 });
            edges.Add(new CanvasEdge { Id = 1, A = 1, B = 2 });
        }
        public bool IsClosed  => true;
    }
}