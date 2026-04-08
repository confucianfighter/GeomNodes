using System;
using System.Collections.Generic;
using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    [Serializable]
    public class ShapeCanvasDocument : ICanvasDocument, ICanvasBoundsProvider
    {
        [SerializeField] private Vector2 worldSizeMeters = new Vector2(1f, 1f);
        [SerializeField] private List<CanvasPoint> points = new();
        [SerializeField] private List<CanvasEdge> edges = new();
        [SerializeField] private List<CanvasOffsetConstraint> offsets = new();

        public Vector2 WorldSizeMeters
        {
            get => worldSizeMeters;
            set => worldSizeMeters = new Vector2(
                Mathf.Max(0.0001f, value.x),
                Mathf.Max(0.0001f, value.y)
            );
        }

        public IList<CanvasPoint> Points => points;
        public IList<CanvasEdge> Edges => edges;
        public IList<CanvasOffsetConstraint> Offsets => offsets;

        public int PointCount => points.Count;
        public bool IsClosed => true;

        public void MarkDirty()
        {
        }

        public void EnsureValidShape()
        {
            WorldSizeMeters = worldSizeMeters;

            if (points.Count < 3)
                ResetToDefaultTriangle();

            ClampAllPointsToWorld();

            if (edges.Count != points.Count)
                RebuildClosedEdges();
        }

        public void ResetToDefaultTriangle()
        {
            points.Clear();
            edges.Clear();
            offsets.Clear();

            points.Add(new CanvasPoint { Id = 0, Position = new Vector2(0.15f, 0.15f) });
            points.Add(new CanvasPoint { Id = 1, Position = new Vector2(0.85f, 0.15f) });
            points.Add(new CanvasPoint { Id = 2, Position = new Vector2(0.50f, 0.80f) });

            RebuildClosedEdges();
        }

        public void DeletePoints(IEnumerable<int> pointIds)
        {
            HashSet<int> ids = new(pointIds);

            if (ids.Count == 0)
                return;

            points.RemoveAll(p => ids.Contains(p.Id));

            if (points.Count < 3)
            {
                ResetToDefaultTriangle();
                return;
            }

            RebuildClosedEdges();
            offsets.Clear();
            ClampAllPointsToWorld();
        }

        public Rect GetCanvasFrameRect()
        {
            return new Rect(0f, 0f, WorldSizeMeters.x, WorldSizeMeters.y);
        }

        private void ClampAllPointsToWorld()
        {
            for (int i = 0; i < points.Count; i++)
            {
                CanvasPoint p = points[i];
                p.Position = new Vector2(
                    Mathf.Clamp(p.Position.x, 0f, WorldSizeMeters.x),
                    Mathf.Clamp(p.Position.y, 0f, WorldSizeMeters.y)
                );
                points[i] = p;
            }
        }

        private void RebuildClosedEdges()
        {
            edges.Clear();

            for (int i = 0; i < points.Count; i++)
            {
                int next = (i + 1) % points.Count;
                edges.Add(new CanvasEdge
                {
                    Id = i,
                    A = points[i].Id,
                    B = points[next].Id
                });
            }
        }
    }
}