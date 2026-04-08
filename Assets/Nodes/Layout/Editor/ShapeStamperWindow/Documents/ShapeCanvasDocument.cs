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

        [SerializeField] private bool hasInnerShape;
        [SerializeField] private List<CanvasPoint> innerPoints = new();
        [SerializeField] private List<CanvasEdge> innerEdges = new();

        public bool HasInnerShape
        {
            get => hasInnerShape;
            set => hasInnerShape = value;
        }
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
        public void EnsureDefaultInnerShape()
        {
            WorldSizeMeters = worldSizeMeters;

            if (innerPoints == null)
                innerPoints = new List<CanvasPoint>();

            if (innerEdges == null)
                innerEdges = new List<CanvasEdge>();

            if (innerPoints.Count >= 3 && innerEdges.Count == innerPoints.Count)
            {
                ClampAllInnerPointsToWorld();
                return;
            }

            innerPoints.Clear();
            innerEdges.Clear();

            Vector2 size = WorldSizeMeters;
            Vector2 center = size * 0.5f;

            float halfWidth = size.x * 0.12f;
            float halfHeight = size.y * 0.12f;

            int nextId = GetNextPointId(points, innerPoints);

            innerPoints.Add(new CanvasPoint
            {
                Id = nextId++,
                Position = new Vector2(center.x, center.y + halfHeight)
            });

            innerPoints.Add(new CanvasPoint
            {
                Id = nextId++,
                Position = new Vector2(center.x - halfWidth, center.y - halfHeight)
            });

            innerPoints.Add(new CanvasPoint
            {
                Id = nextId++,
                Position = new Vector2(center.x + halfWidth, center.y - halfHeight)
            });

            RebuildClosedInnerEdges();
            ClampAllInnerPointsToWorld();
        }
        private void ClampAllInnerPointsToWorld()
        {
            for (int i = 0; i < innerPoints.Count; i++)
            {
                CanvasPoint p = innerPoints[i];
                p.Position = new Vector2(
                    Mathf.Clamp(p.Position.x, 0f, WorldSizeMeters.x),
                    Mathf.Clamp(p.Position.y, 0f, WorldSizeMeters.y)
                );
                innerPoints[i] = p;
            }
        }

        private void RebuildClosedInnerEdges()
        {
            innerEdges.Clear();

            for (int i = 0; i < innerPoints.Count; i++)
            {
                int next = (i + 1) % innerPoints.Count;
                innerEdges.Add(new CanvasEdge
                {
                    Id = i,
                    A = innerPoints[i].Id,
                    B = innerPoints[next].Id
                });
            }
        }

        private static int GetNextPointId(IList<CanvasPoint> outerPoints, IList<CanvasPoint> innerPoints)
        {
            int maxId = -1;

            if (outerPoints != null)
            {
                for (int i = 0; i < outerPoints.Count; i++)
                    maxId = Mathf.Max(maxId, outerPoints[i].Id);
            }

            if (innerPoints != null)
            {
                for (int i = 0; i < innerPoints.Count; i++)
                    maxId = Mathf.Max(maxId, innerPoints[i].Id);
            }

            return maxId + 1;
        }
    }
}