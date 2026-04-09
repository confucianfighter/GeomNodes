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

        public enum ShapeLoopEditMode
        {
            Outer = 0,
            Inner = 1
        }

        [SerializeField] private ShapeLoopEditMode editMode = ShapeLoopEditMode.Outer;

        public ShapeLoopEditMode EditMode
        {
            get => editMode;
            set => editMode = value;
        }

        public bool HasInnerShape
        {
            get => hasInnerShape;
            set
            {
                hasInnerShape = value;
                if (!hasInnerShape && editMode == ShapeLoopEditMode.Inner)
                    editMode = ShapeLoopEditMode.Outer;
            }
        }

        public Vector2 WorldSizeMeters
        {
            get => worldSizeMeters;
            set => worldSizeMeters = new Vector2(
                Mathf.Max(0.0001f, value.x),
                Mathf.Max(0.0001f, value.y)
            );
        }

        public bool IsEditingInnerLoop => HasInnerShape && EditMode == ShapeLoopEditMode.Inner;

        public IList<CanvasPoint> Points => IsEditingInnerLoop ? innerPoints : points;
        public IList<CanvasEdge> Edges => IsEditingInnerLoop ? innerEdges : edges;
        public IList<CanvasOffsetConstraint> Offsets => offsets;

        public IList<CanvasPoint> OuterPoints => points;
        public IList<CanvasEdge> OuterEdges => edges;
        public IList<CanvasPoint> InnerPoints => innerPoints;
        public IList<CanvasEdge> InnerEdges => innerEdges;

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

            if (HasInnerShape)
            {
                if (innerPoints == null)
                    innerPoints = new List<CanvasPoint>();

                if (innerEdges == null)
                    innerEdges = new List<CanvasEdge>();

                if (innerPoints.Count < 3)
                    EnsureDefaultInnerShape();
                else
                {
                    ClampAllInnerPointsToWorld();

                    if (innerEdges.Count != innerPoints.Count)
                        RebuildClosedInnerEdges();
                }
            }
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

            List<CanvasPoint> activePoints = IsEditingInnerLoop ? innerPoints : points;
            List<CanvasEdge> activeEdges = IsEditingInnerLoop ? innerEdges : edges;

            activePoints.RemoveAll(p => ids.Contains(p.Id));

            if (activePoints.Count < 3)
            {
                if (IsEditingInnerLoop)
                {
                    innerPoints.Clear();
                    innerEdges.Clear();
                    EnsureDefaultInnerShape();
                }
                else
                {
                    ResetToDefaultTriangle();
                }
                return;
            }

            if (IsEditingInnerLoop)
            {
                RebuildClosedInnerEdges();
                ClampAllInnerPointsToWorld();
            }
            else
            {
                RebuildClosedEdges();
                offsets.Clear();
                ClampAllPointsToWorld();
            }
        }

        public Rect GetCanvasFrameRect()
        {
            return new Rect(0f, 0f, WorldSizeMeters.x, WorldSizeMeters.y);
        }

        public void ResizeWorld(Vector2 newSize)
        {
            Rect oldBounds = GetCanvasFrameRect();

            WorldSizeMeters = new Vector2(
                Mathf.Max(0.0001f, newSize.x),
                Mathf.Max(0.0001f, newSize.y)
            );

            Rect newBounds = GetCanvasFrameRect();

            ResizePointList(points, oldBounds, newBounds);
            ResizePointList(innerPoints, oldBounds, newBounds);
        }

        private static void ResizePointList(List<CanvasPoint> list, Rect oldBounds, Rect newBounds)
        {
            if (list == null)
                return;

            for (int i = 0; i < list.Count; i++)
            {
                CanvasPoint p = list[i];
                ShapeCanvasPointResolver.ResizePointPreservingBehavior(ref p, oldBounds, newBounds);
                list[i] = p;
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

            int nextId = GetNextPointIdAcrossAll();

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

        private void RebuildClosedInnerEdges()
        {
            innerEdges.Clear();
            int nextEdgeId = GetNextEdgeIdFromOuter();

            for (int i = 0; i < innerPoints.Count; i++)
            {
                int next = (i + 1) % innerPoints.Count;
                innerEdges.Add(new CanvasEdge
                {
                    Id = nextEdgeId++,
                    A = innerPoints[i].Id,
                    B = innerPoints[next].Id
                });
            }
        }

        private int GetNextPointIdAcrossAll()
        {
            int maxId = -1;

            for (int i = 0; i < points.Count; i++)
                maxId = Mathf.Max(maxId, points[i].Id);

            for (int i = 0; i < innerPoints.Count; i++)
                maxId = Mathf.Max(maxId, innerPoints[i].Id);

            return maxId + 1;
        }

        private int GetNextEdgeIdFromOuter()
        {
            int maxId = -1;

            for (int i = 0; i < edges.Count; i++)
                maxId = Mathf.Max(maxId, edges[i].Id);

            return maxId + 1;
        }
    }
}
