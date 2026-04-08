using System;
using System.Collections.Generic;
using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    [Serializable]
    public class ProfileCanvasDocument : ICanvasDocument, ICanvasBoundsProvider
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

        public bool IsClosed => false;

        public void MarkDirty()
        {
        }

        public void EnsureValidProfile()
        {
            WorldSizeMeters = worldSizeMeters;

            if (points.Count == 0)
                ResetDefaultProfile();

            ClampAllPointsToWorld();

            if (edges.Count == 0 && points.Count >= 2)
                RebuildOpenEdges();
        }

        public void ResetDefaultProfile()
        {
            points.Clear();
            edges.Clear();
            offsets.Clear();

            points.Add(new CanvasPoint { Id = 0, Position = new Vector2(0.10f, 0.10f) });
            points.Add(new CanvasPoint { Id = 1, Position = new Vector2(0.30f, 0.20f) });
            points.Add(new CanvasPoint { Id = 2, Position = new Vector2(0.50f, 0.50f) });

            RebuildOpenEdges();
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

        private void RebuildOpenEdges()
        {
            edges.Clear();

            for (int i = 0; i < points.Count - 1; i++)
            {
                edges.Add(new CanvasEdge
                {
                    Id = i,
                    A = points[i].Id,
                    B = points[i + 1].Id
                });
            }
        }
    }
}