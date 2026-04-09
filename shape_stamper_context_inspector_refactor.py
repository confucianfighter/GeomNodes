from pathlib import Path

script = r'''#!/usr/bin/env python3
from pathlib import Path
import argparse
import sys

FILES = {
    "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Model/CanvasAnchorX.cs": """using System;

namespace DLN.EditorTools.ShapeStamper
{
    [Serializable]
    public enum CanvasAnchorX
    {
        None = 0,
        Left = 1,
        Center = 2,
        Right = 3
    }
}
""",
    "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Model/CanvasAnchorY.cs": """using System;

namespace DLN.EditorTools.ShapeStamper
{
    [Serializable]
    public enum CanvasAnchorY
    {
        None = 0,
        Bottom = 1,
        Center = 2,
        Top = 3
    }
}
""",
    "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Model/CanvasPoint.cs": """using System;
using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
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

            XAnchor = CanvasAnchorX.None;
            YAnchor = CanvasAnchorY.None;

            OffsetX = 0f;
            OffsetY = 0f;
        }
    }
}
""",
    "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Model/CanvasEdge.cs": """using System;

namespace DLN.EditorTools.ShapeStamper
{
    [Serializable]
    public struct CanvasEdge
    {
        public int Id;
        public int A;
        public int B;
    }
}
""",
    "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Model/CanvasOffsetConstraint.cs": """using System;

namespace DLN.EditorTools.ShapeStamper
{
    [Serializable]
    public struct CanvasOffsetConstraint
    {
        public int Id;
        public int EdgeId;
        public float Distance;
    }
}
""",
    "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Model/CanvasElementType.cs": """namespace DLN.EditorTools.ShapeStamper
{
    public enum CanvasElementType
    {
        None = 0,
        Point = 1,
        Edge = 2,
        Offset = 3
    }
}
""",
    "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Model/CanvasElementRef.cs": """using System;
using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
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
""",
    "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Core/ShapeCanvasPointResolver.cs": """using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    public static class ShapeCanvasPointResolver
    {
        public static Vector2 ResolvePoint(
            CanvasPoint point,
            Rect oldBounds,
            Rect newBounds)
        {
            float x = ResolveX(point, oldBounds, newBounds);
            float y = ResolveY(point, oldBounds, newBounds);
            return new Vector2(x, y);
        }

        public static float ResolveX(CanvasPoint point, Rect oldBounds, Rect newBounds)
        {
            switch (point.XAnchor)
            {
                case CanvasAnchorX.Left:
                    return newBounds.xMin + point.OffsetX;

                case CanvasAnchorX.Center:
                    return newBounds.center.x + point.OffsetX;

                case CanvasAnchorX.Right:
                    return newBounds.xMax + point.OffsetX;

                case CanvasAnchorX.None:
                default:
                    return RemapPreservingRatio(
                        point.Position.x,
                        oldBounds.xMin,
                        oldBounds.xMax,
                        newBounds.xMin,
                        newBounds.xMax);
            }
        }

        public static float ResolveY(CanvasPoint point, Rect oldBounds, Rect newBounds)
        {
            switch (point.YAnchor)
            {
                case CanvasAnchorY.Bottom:
                    return newBounds.yMin + point.OffsetY;

                case CanvasAnchorY.Center:
                    return newBounds.center.y + point.OffsetY;

                case CanvasAnchorY.Top:
                    return newBounds.yMax + point.OffsetY;

                case CanvasAnchorY.None:
                default:
                    return RemapPreservingRatio(
                        point.Position.y,
                        oldBounds.yMin,
                        oldBounds.yMax,
                        newBounds.yMin,
                        newBounds.yMax);
            }
        }

        private static float RemapPreservingRatio(
            float value,
            float oldMin,
            float oldMax,
            float newMin,
            float newMax)
        {
            float oldSize = oldMax - oldMin;
            if (Mathf.Abs(oldSize) < 0.0001f)
                return newMin;

            float t = (value - oldMin) / oldSize;
            return Mathf.Lerp(newMin, newMax, t);
        }
    }
}
""",
    "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Core/ShapeCanvasConstraintUtility.cs": """using System.Collections.Generic;
using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    public static class ShapeCanvasConstraintUtility
    {
        public static void AssignAnchorsFromCurrentPosition(
            ref CanvasPoint point,
            Rect bounds,
            CanvasAnchorX xAnchor,
            CanvasAnchorY yAnchor)
        {
            point.XAnchor = xAnchor;
            point.YAnchor = yAnchor;
            RecalculateOffsetsFromPosition(ref point, bounds);
        }

        public static void RecalculateOffsetsFromPosition(
            ref CanvasPoint point,
            Rect bounds)
        {
            point.OffsetX = point.XAnchor switch
            {
                CanvasAnchorX.Left => point.Position.x - bounds.xMin,
                CanvasAnchorX.Center => point.Position.x - bounds.center.x,
                CanvasAnchorX.Right => point.Position.x - bounds.xMax,
                _ => point.OffsetX
            };

            point.OffsetY = point.YAnchor switch
            {
                CanvasAnchorY.Bottom => point.Position.y - bounds.yMin,
                CanvasAnchorY.Center => point.Position.y - bounds.center.y,
                CanvasAnchorY.Top => point.Position.y - bounds.yMax,
                _ => point.OffsetY
            };
        }

        public static void SetPointPosition(
            ref CanvasPoint point,
            Vector2 newPosition,
            Rect bounds)
        {
            point.Position = newPosition;
            RecalculateOffsetsFromPosition(ref point, bounds);
        }

        public static void ResolvePositionFromOffsets(
            ref CanvasPoint point,
            Rect bounds)
        {
            float x = point.Position.x;
            float y = point.Position.y;

            switch (point.XAnchor)
            {
                case CanvasAnchorX.Left:
                    x = bounds.xMin + point.OffsetX;
                    break;
                case CanvasAnchorX.Center:
                    x = bounds.center.x + point.OffsetX;
                    break;
                case CanvasAnchorX.Right:
                    x = bounds.xMax + point.OffsetX;
                    break;
            }

            switch (point.YAnchor)
            {
                case CanvasAnchorY.Bottom:
                    y = bounds.yMin + point.OffsetY;
                    break;
                case CanvasAnchorY.Center:
                    y = bounds.center.y + point.OffsetY;
                    break;
                case CanvasAnchorY.Top:
                    y = bounds.yMax + point.OffsetY;
                    break;
            }

            point.Position = new Vector2(x, y);
        }

        public static void ResolvePointIntoPosition(
            ref CanvasPoint point,
            Rect oldBounds,
            Rect newBounds)
        {
            point.Position = ShapeCanvasPointResolver.ResolvePoint(point, oldBounds, newBounds);
            RecalculateOffsetsFromPosition(ref point, newBounds);
        }

        public static void ResolveAllPointsIntoPosition(
            IList<CanvasPoint> points,
            Rect oldBounds,
            Rect newBounds)
        {
            if (points == null)
                return;

            for (int i = 0; i < points.Count; i++)
            {
                CanvasPoint point = points[i];
                ResolvePointIntoPosition(ref point, oldBounds, newBounds);
                points[i] = point;
            }
        }

        public static void ClampPointToBounds(
            ref CanvasPoint point,
            Rect bounds)
        {
            point.Position = new Vector2(
                Mathf.Clamp(point.Position.x, bounds.xMin, bounds.xMax),
                Mathf.Clamp(point.Position.y, bounds.yMin, bounds.yMax)
            );

            RecalculateOffsetsFromPosition(ref point, bounds);
        }
    }
}
""",
    "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Documents/ShapeCanvasDocument.cs": """using System;
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

        public void ResizeWorld(Vector2 newWorldSize)
        {
            Rect oldBounds = GetCanvasFrameRect();
            WorldSizeMeters = newWorldSize;
            Rect newBounds = GetCanvasFrameRect();

            ShapeCanvasConstraintUtility.ResolveAllPointsIntoPosition(points, oldBounds, newBounds);

            if (HasInnerShape)
                ShapeCanvasConstraintUtility.ResolveAllPointsIntoPosition(innerPoints, oldBounds, newBounds);

            ClampAllPointsToWorld();
            ClampAllInnerPointsToWorld();
        }

        public bool TryGetPointById(int id, out CanvasPoint point)
        {
            List<CanvasPoint> activePoints = IsEditingInnerLoop ? innerPoints : points;

            for (int i = 0; i < activePoints.Count; i++)
            {
                if (activePoints[i].Id == id)
                {
                    point = activePoints[i];
                    return true;
                }
            }

            point = default;
            return false;
        }

        public bool SetPoint(CanvasPoint updatedPoint)
        {
            List<CanvasPoint> activePoints = IsEditingInnerLoop ? innerPoints : points;

            for (int i = 0; i < activePoints.Count; i++)
            {
                if (activePoints[i].Id != updatedPoint.Id)
                    continue;

                activePoints[i] = updatedPoint;
                return true;
            }

            return false;
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
            Rect bounds = GetCanvasFrameRect();

            for (int i = 0; i < points.Count; i++)
            {
                CanvasPoint p = points[i];
                ShapeCanvasConstraintUtility.ClampPointToBounds(ref p, bounds);
                points[i] = p;
            }
        }

        private void ClampAllInnerPointsToWorld()
        {
            if (innerPoints == null)
                return;

            Rect bounds = GetCanvasFrameRect();

            for (int i = 0; i < innerPoints.Count; i++)
            {
                CanvasPoint p = innerPoints[i];
                ShapeCanvasConstraintUtility.ClampPointToBounds(ref p, bounds);
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
""",
    "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Documents/ProfileCanvasDocument.cs": """using System;
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

        public void ResizeWorld(Vector2 newWorldSize)
        {
            Rect oldBounds = GetCanvasFrameRect();
            WorldSizeMeters = newWorldSize;
            Rect newBounds = GetCanvasFrameRect();

            ShapeCanvasConstraintUtility.ResolveAllPointsIntoPosition(points, oldBounds, newBounds);
            ClampAllPointsToWorld();
        }

        public bool TryGetPointById(int id, out CanvasPoint point)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].Id == id)
                {
                    point = points[i];
                    return true;
                }
            }

            point = default;
            return false;
        }

        public bool SetPoint(CanvasPoint updatedPoint)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].Id != updatedPoint.Id)
                    continue;

                points[i] = updatedPoint;
                return true;
            }

            return false;
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
            Rect bounds = GetCanvasFrameRect();

            for (int i = 0; i < points.Count; i++)
            {
                CanvasPoint p = points[i];
                ShapeCanvasConstraintUtility.ClampPointToBounds(ref p, bounds);
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
""",
    "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Core/EditorCanvas.cs": """#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    [Serializable]
    public class EditorCanvas
    {
        private const float DefaultPointRadius = 6f;
        private const float DefaultEdgeHitDistance = 8f;
        private const float DefaultOffsetHitDistance = 10f;
        private const float DragStartThreshold = 4f;

        public ICanvasDocument Document { get; private set; }
        public ICanvasToolPolicy Policy { get; private set; }

        public CanvasSelection Selection { get; private set; }
        public CanvasInteractionState Interaction { get; private set; }
        public CanvasViewState View { get; private set; }

        public Rect LastCanvasRect { get; private set; }

        public EditorCanvas(
            ICanvasDocument document,
            ICanvasToolPolicy policy,
            CanvasSelection selection,
            CanvasInteractionState interaction,
            CanvasViewState view)
        {
            Document = document;
            Policy = policy;
            Selection = selection;
            Interaction = interaction;
            View = view;
        }

        public void SetDocument(ICanvasDocument document)
        {
            Document = document;
            ClearTransientState();
            View?.ResetView();
        }

        public void SetPolicy(ICanvasToolPolicy policy)
        {
            Policy = policy;
        }

        public void Draw(Rect canvasRect)
        {
            LastCanvasRect = canvasRect;

            if (Document == null)
            {
                EditorGUI.HelpBox(canvasRect, "No canvas document assigned.", MessageType.Info);
                return;
            }

            Event evt = Event.current;
            HandleEvent(evt, canvasRect);

            DrawBackground(canvasRect);
            DrawWorldMask(canvasRect);
            DrawGrid(canvasRect);

            CanvasDrawing.DrawDocument(
                canvasRect: canvasRect,
                document: Document,
                selection: Selection,
                interaction: Interaction,
                view: View);

            DrawWorldBounds(canvasRect);
            DrawMarquee();

            Policy?.DrawOverlay(this, canvasRect);

            if (evt.type == EventType.Repaint)
                EditorGUIUtility.AddCursorRect(canvasRect, MouseCursor.Arrow);
        }

        public Vector2 ScreenToCanvas(Vector2 screenPosition)
        {
            return CanvasMath.ScreenToCanvas(screenPosition, LastCanvasRect, View, Document);
        }

        private void DrawMarquee()
        {
            if (!Interaction.IsMarqueeSelecting)
                return;

            Rect marquee = GetScreenMarqueeRect();

            EditorGUI.DrawRect(marquee, new Color(0.3f, 0.6f, 1f, 0.10f));

            Handles.BeginGUI();
            Color old = Handles.color;
            Handles.color = new Color(0.3f, 0.6f, 1f, 0.9f);
            Handles.DrawAAPolyLine(
                2f,
                new Vector3(marquee.xMin, marquee.yMin),
                new Vector3(marquee.xMax, marquee.yMin),
                new Vector3(marquee.xMax, marquee.yMax),
                new Vector3(marquee.xMin, marquee.yMax),
                new Vector3(marquee.xMin, marquee.yMin));
            Handles.color = old;
            Handles.EndGUI();
        }

        public Vector2 CanvasToScreen(Vector2 canvasPosition)
        {
            return CanvasMath.CanvasToScreen(canvasPosition, LastCanvasRect, View, Document);
        }

        public void FrameAll(float padding = 24f)
        {
            if (View != null)
                View.WorldPaddingPixels = padding;
        }

        public void ClearSelection()
        {
            Selection?.Clear();
        }

        public void ClearTransientState()
        {
            if (Interaction == null)
                return;

            Interaction.Hovered = default;
            Interaction.Pressed = default;
            Interaction.Dragging = default;
            Interaction.IsDragging = false;
            Interaction.IsDraggingPoints = false;
            Interaction.IsPanning = false;
            Interaction.IsMarqueeSelecting = false;
            Interaction.DragPointStartPositions.Clear();
            Interaction.MarqueeSelectionSnapshot.Clear();
        }

        private void HandleEvent(Event evt, Rect canvasRect)
        {
            if (!canvasRect.Contains(evt.mousePosition) &&
                evt.type != EventType.MouseUp &&
                evt.type != EventType.MouseDrag)
            {
                return;
            }

            UpdateHover(evt.mousePosition);

            switch (evt.type)
            {
                case EventType.MouseDown:
                    HandleMouseDown(evt);
                    break;

                case EventType.MouseDrag:
                    HandleMouseDrag(evt);
                    break;

                case EventType.MouseUp:
                    HandleMouseUp(evt);
                    break;

                case EventType.KeyDown:
                    HandleKeyDown(evt);
                    break;
            }
        }

        private void HandleMouseDown(Event evt)
        {
            GUI.FocusControl(null);

            if (evt.button == 1)
            {
                HandleContextMouseDown(evt);
                return;
            }

            if (evt.button != 0)
                return;

            Interaction.MouseDownScreen = evt.mousePosition;
            Interaction.MouseDownCanvas = ScreenToCanvas(evt.mousePosition);
            Interaction.MouseDownElement = Interaction.Hovered;
            Interaction.Pressed = Interaction.Hovered;
            Interaction.IsDragging = false;

            bool additive = evt.shift;
            bool subtractive = evt.control || evt.command;

            if (Interaction.Hovered.IsValid)
            {
                if (!Selection.Contains(Interaction.Hovered))
                {
                    if (!additive && !subtractive)
                        Selection.Clear();

                    if (subtractive)
                        Selection.Remove(Interaction.Hovered);
                    else
                        Selection.Add(Interaction.Hovered);
                }

                if (Interaction.Hovered.Type == CanvasElementType.Point)
                    BeginPointDrag();

                Policy?.OnMouseDown(this, evt);
                evt.Use();
                return;
            }

            if (!additive && !subtractive)
                Selection.Clear();

            BeginMarquee(evt.mousePosition);
            Policy?.OnMouseDown(this, evt);
            evt.Use();
        }

        private void HandleMouseDrag(Event evt)
        {
            float dragDistance = Vector2.Distance(evt.mousePosition, Interaction.MouseDownScreen);
            if (!Interaction.IsDragging && dragDistance >= DragStartThreshold)
                Interaction.IsDragging = true;

            if (Interaction.IsDraggingPoints)
            {
                DragSelectedPoints(evt.mousePosition);
                Policy?.OnDrag(this, evt);
                evt.Use();
                return;
            }

            if (Interaction.IsMarqueeSelecting)
            {
                Interaction.MarqueeEndScreen = evt.mousePosition;
                UpdateMarqueeSelection(evt.shift, evt.control || evt.command);
                Policy?.OnDrag(this, evt);
                evt.Use();
                return;
            }

            Policy?.OnDrag(this, evt);
        }

        private void HandleMouseUp(Event evt)
        {
            if (evt.button != 0)
                return;

            bool wasDragging = Interaction.IsDragging;

            if (Interaction.IsDraggingPoints)
            {
                EndPointDrag();
                Document.MarkDirty();
                evt.Use();
            }
            else if (Interaction.IsMarqueeSelecting)
            {
                EndMarquee();
                evt.Use();
            }
            else if (!wasDragging)
            {
                Policy?.OnClick(this, evt);
                evt.Use();
            }

            Interaction.Pressed = default;
            Interaction.IsDragging = false;
        }

        private void HandleKeyDown(Event evt)
        {
            if (evt.keyCode == KeyCode.A && (evt.control || evt.command))
            {
                SelectAll();
                evt.Use();
                return;
            }

            if (evt.keyCode == KeyCode.Escape)
            {
                Selection.Clear();
                ClearTransientState();
                evt.Use();
                return;
            }

            if (evt.keyCode == KeyCode.F)
            {
                View?.ResetView();
                evt.Use();
                return;
            }

            Policy?.OnKeyDown(this, evt);
        }

        private void HandleContextMouseDown(Event evt)
        {
            GenericMenu menu = new GenericMenu();

            bool isProfileCanvas = Document is ProfileCanvasDocument;
            bool isShapeCanvas = Document is ShapeCanvasDocument;

            if (Interaction.Hovered.IsValid)
            {
                if (Interaction.Hovered.Type == CanvasElementType.Edge)
                {
                    menu.AddItem(new GUIContent("Split Edge"), false, () =>
                    {
                        Policy?.SplitEdgeAtScreenPosition(this, Interaction.Hovered, evt.mousePosition);
                    });
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Split Edge"));
                }

                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Delete Selection"), false, () =>
                {
                    Policy?.DeleteSelection(this);
                });
            }
            else
            {
                if (isProfileCanvas)
                {
                    menu.AddItem(new GUIContent("Add Segment To End"), false, () =>
                    {
                        Policy?.AddPointAtCanvasPosition(this, ScreenToCanvas(evt.mousePosition));
                    });
                }
                else if (isShapeCanvas)
                {
                    menu.AddDisabledItem(new GUIContent("Split Edge"));
                }

                menu.AddSeparator("");
                menu.AddDisabledItem(new GUIContent("Delete Selection"));
            }

            menu.ShowAsContext();
            evt.Use();
        }

        private void UpdateHover(Vector2 mouseScreen)
        {
            Interaction.LastMouseScreen = mouseScreen;
            Interaction.LastMouseCanvas = ScreenToCanvas(mouseScreen);
            Interaction.Hovered = FindHit(mouseScreen);
        }

        private CanvasElementRef FindHit(Vector2 mouseScreen)
        {
            CanvasElementRef pointHit = FindPointHit(mouseScreen);
            if (pointHit.IsValid)
                return pointHit;

            CanvasElementRef edgeHit = FindEdgeHit(mouseScreen);
            if (edgeHit.IsValid)
                return edgeHit;

            CanvasElementRef offsetHit = FindOffsetHit(mouseScreen);
            if (offsetHit.IsValid)
                return offsetHit;

            return default;
        }

        private CanvasElementRef FindPointHit(Vector2 mouseScreen)
        {
            float bestDistance = DefaultPointRadius + 4f;
            CanvasElementRef best = default;

            foreach (CanvasPoint point in Document.Points)
            {
                Vector2 pointScreen = CanvasToScreen(point.Position);
                float distance = Vector2.Distance(mouseScreen, pointScreen);
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    best = CanvasElementRef.ForPoint(point.Id);
                }
            }

            return best;
        }

        private CanvasElementRef FindEdgeHit(Vector2 mouseScreen)
        {
            float bestDistance = DefaultEdgeHitDistance;
            CanvasElementRef best = default;

            foreach (CanvasEdge edge in Document.Edges)
            {
                if (!TryGetEdgeScreenPositions(edge, out Vector2 a, out Vector2 b))
                    continue;

                float distance = CanvasMath.DistancePointToSegment(mouseScreen, a, b);
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    best = CanvasElementRef.ForEdge(edge.Id);
                }
            }

            return best;
        }

        private CanvasElementRef FindOffsetHit(Vector2 mouseScreen)
        {
            float bestDistance = DefaultOffsetHitDistance;
            CanvasElementRef best = default;

            foreach (CanvasOffsetConstraint offset in Document.Offsets)
            {
                if (!CanvasMath.TryGetOffsetHandleScreenPosition(
                        offset,
                        Document,
                        LastCanvasRect,
                        View,
                        out Vector2 handleScreen))
                    continue;

                float distance = Vector2.Distance(mouseScreen, handleScreen);
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    best = CanvasElementRef.ForOffset(offset.Id);
                }
            }

            return best;
        }

        private void BeginPointDrag()
        {
            Interaction.IsDraggingPoints = true;
            Interaction.DragStartCanvas = Interaction.MouseDownCanvas;
            Interaction.DragPointStartPositions.Clear();

            foreach (CanvasElementRef selected in Selection.Elements)
            {
                if (selected.Type != CanvasElementType.Point)
                    continue;

                if (TryGetPointById(selected.Id, out CanvasPoint point))
                    Interaction.DragPointStartPositions[selected.Id] = point.Position;
            }
        }

        private void DragSelectedPoints(Vector2 currentMouseScreen)
        {
            Vector2 currentCanvas = ScreenToCanvas(currentMouseScreen);
            Vector2 delta = currentCanvas - Interaction.DragStartCanvas;

            foreach ((int pointId, Vector2 startPos) in Interaction.DragPointStartPositions)
            {
                Vector2 newPos = startPos + delta;
                Policy?.ConstrainDraggedPoint(this, pointId, ref newPos);
                SetPointPosition(pointId, newPos);
            }
        }

        private void EndPointDrag()
        {
            Interaction.IsDraggingPoints = false;
            Interaction.DragPointStartPositions.Clear();
        }

        private void BeginMarquee(Vector2 mouseScreen)
        {
            Interaction.IsMarqueeSelecting = true;
            Interaction.MarqueeStartScreen = mouseScreen;
            Interaction.MarqueeEndScreen = mouseScreen;
            Interaction.MarqueeSelectionSnapshot = Selection.CloneElements();
        }

        private void UpdateMarqueeSelection(bool additive, bool subtractive)
        {
            Rect marquee = GetScreenMarqueeRect();

            if (!additive && !subtractive)
                Selection.Clear();
            else
                Selection.SetElements(Interaction.MarqueeSelectionSnapshot);

            foreach (CanvasPoint point in Document.Points)
            {
                Vector2 pointScreen = CanvasToScreen(point.Position);
                if (!marquee.Contains(pointScreen))
                    continue;

                CanvasElementRef pointRef = CanvasElementRef.ForPoint(point.Id);

                if (subtractive)
                    Selection.Remove(pointRef);
                else
                    Selection.Add(pointRef);
            }
        }

        private void EndMarquee()
        {
            Interaction.IsMarqueeSelecting = false;
        }

        private void SelectAll()
        {
            Selection.Clear();

            foreach (CanvasPoint point in Document.Points)
                Selection.Add(CanvasElementRef.ForPoint(point.Id));

            foreach (CanvasEdge edge in Document.Edges)
                Selection.Add(CanvasElementRef.ForEdge(edge.Id));

            foreach (CanvasOffsetConstraint offset in Document.Offsets)
                Selection.Add(CanvasElementRef.ForOffset(offset.Id));
        }

        private Rect GetScreenMarqueeRect()
        {
            Vector2 min = Vector2.Min(Interaction.MarqueeStartScreen, Interaction.MarqueeEndScreen);
            Vector2 max = Vector2.Max(Interaction.MarqueeStartScreen, Interaction.MarqueeEndScreen);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private void DrawBackground(Rect canvasRect)
        {
            EditorGUI.DrawRect(canvasRect, new Color(0.13f, 0.13f, 0.13f));
        }

        private void DrawWorldMask(Rect canvasRect)
        {
            Rect worldScreenRect = CanvasMath.GetFittedWorldScreenRect(canvasRect, View, Document);

            Color outside = new Color(0f, 0f, 0f, 0.18f);

            Rect top = new Rect(canvasRect.xMin, canvasRect.yMin, canvasRect.width, Mathf.Max(0f, worldScreenRect.yMin - canvasRect.yMin));
            Rect bottom = new Rect(canvasRect.xMin, worldScreenRect.yMax, canvasRect.width, Mathf.Max(0f, canvasRect.yMax - worldScreenRect.yMax));
            Rect left = new Rect(canvasRect.xMin, worldScreenRect.yMin, Mathf.Max(0f, worldScreenRect.xMin - canvasRect.xMin), Mathf.Max(0f, worldScreenRect.height));
            Rect right = new Rect(worldScreenRect.xMax, worldScreenRect.yMin, Mathf.Max(0f, canvasRect.xMax - worldScreenRect.xMax), Mathf.Max(0f, worldScreenRect.height));

            if (top.width > 0f && top.height > 0f) EditorGUI.DrawRect(top, outside);
            if (bottom.width > 0f && bottom.height > 0f) EditorGUI.DrawRect(bottom, outside);
            if (left.width > 0f && left.height > 0f) EditorGUI.DrawRect(left, outside);
            if (right.width > 0f && right.height > 0f) EditorGUI.DrawRect(right, outside);
        }

        private void DrawGrid(Rect canvasRect)
        {
            Rect worldRect = CanvasMath.GetWorldRect(Document);
            Rect worldScreenRect = CanvasMath.GetFittedWorldScreenRect(canvasRect, View, Document);

            float pixelsPerWorldUnitX = worldScreenRect.width / Mathf.Max(0.0001f, worldRect.width);
            float pixelsPerWorldUnitY = worldScreenRect.height / Mathf.Max(0.0001f, worldRect.height);
            float pixelsPerWorldUnit = Mathf.Min(pixelsPerWorldUnitX, pixelsPerWorldUnitY);

            float targetPixels = 48f;
            float step = GetNiceWorldStep(targetPixels / Mathf.Max(0.0001f, pixelsPerWorldUnit));

            Handles.BeginGUI();

            Color oldColor = Handles.color;
            Color minor = new Color(1f, 1f, 1f, 0.06f);
            Color major = new Color(1f, 1f, 1f, 0.12f);

            float startX = Mathf.Ceil(worldRect.xMin / step) * step;
            float endX = worldRect.xMax + step * 0.5f;
            float startY = Mathf.Ceil(worldRect.yMin / step) * step;
            float endY = worldRect.yMax + step * 0.5f;

            int ix = 0;
            for (float x = startX; x <= endX; x += step, ix++)
            {
                Vector2 a = CanvasToScreen(new Vector2(x, worldRect.yMin));
                Vector2 b = CanvasToScreen(new Vector2(x, worldRect.yMax));
                Handles.color = (ix % 5 == 0) ? major : minor;
                Handles.DrawLine(a, b);
            }

            int iy = 0;
            for (float y = startY; y <= endY; y += step, iy++)
            {
                Vector2 a = CanvasToScreen(new Vector2(worldRect.xMin, y));
                Vector2 b = CanvasToScreen(new Vector2(worldRect.xMax, y));
                Handles.color = (iy % 5 == 0) ? major : minor;
                Handles.DrawLine(a, b);
            }

            Handles.color = oldColor;
            Handles.EndGUI();
        }

        private void DrawWorldBounds(Rect canvasRect)
        {
            Rect screenRect = CanvasMath.GetFittedWorldScreenRect(canvasRect, View, Document);

            Vector2 bl = new Vector2(screenRect.xMin, screenRect.yMin);
            Vector2 br = new Vector2(screenRect.xMax, screenRect.yMin);
            Vector2 tr = new Vector2(screenRect.xMax, screenRect.yMax);
            Vector2 tl = new Vector2(screenRect.xMin, screenRect.yMax);

            Handles.BeginGUI();
            Color old = Handles.color;

            Handles.color = new Color(1f, 1f, 1f, 0.7f);
            Handles.DrawAAPolyLine(2f, bl, br, tr, tl, bl);

            Handles.color = old;
            Handles.EndGUI();
        }

        private static float GetNiceWorldStep(float rawStep)
        {
            rawStep = Mathf.Max(0.000001f, rawStep);

            float exponent = Mathf.Floor(Mathf.Log10(rawStep));
            float magnitude = Mathf.Pow(10f, exponent);
            float normalized = rawStep / magnitude;

            float niceNormalized;
            if (normalized <= 1f) niceNormalized = 1f;
            else if (normalized <= 2f) niceNormalized = 2f;
            else if (normalized <= 5f) niceNormalized = 5f;
            else niceNormalized = 10f;

            return niceNormalized * magnitude;
        }

        private bool TryGetPointById(int pointId, out CanvasPoint point)
        {
            foreach (CanvasPoint p in Document.Points)
            {
                if (p.Id == pointId)
                {
                    point = p;
                    return true;
                }
            }

            point = default;
            return false;
        }

        private void SetPointPosition(int pointId, Vector2 position)
        {
            Rect bounds = CanvasMath.GetWorldRect(Document);

            for (int i = 0; i < Document.Points.Count; i++)
            {
                if (Document.Points[i].Id != pointId)
                    continue;

                CanvasPoint p = Document.Points[i];
                ShapeCanvasConstraintUtility.SetPointPosition(ref p, position, bounds);
                Document.Points[i] = p;
                return;
            }
        }

        private bool TryGetEdgeScreenPositions(CanvasEdge edge, out Vector2 a, out Vector2 b)
        {
            a = default;
            b = default;

            if (!TryGetPointById(edge.A, out CanvasPoint pointA))
                return false;

            if (!TryGetPointById(edge.B, out CanvasPoint pointB))
                return false;

            a = CanvasToScreen(pointA.Position);
            b = CanvasToScreen(pointB.Position);
            return true;
        }
    }
}
#endif
""",
    "Assets/Nodes/Layout/Editor/ShapeStamperWindow/ShapeStamperWindow.cs": """using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    public class ShapeStamperWindow : EditorWindow
    {
        private const float TopBarHeight = 340f;
        private const float CanvasPadding = 12f;

        private const float DividerWidth = 6f;
        private const float DividerHitWidth = 12f;
        private const float MinPanelWidth = 120f;

        [SerializeField] private ShapeCanvasDocument shapeDocument = new();
        [SerializeField] private ProfileCanvasDocument profileDocument = new();
        [SerializeField] private Vector2 canvasPercentOfWindow = new Vector2(0.8f, 0.8f);
        [SerializeField] private float verticalDividerPercent = 0.65f;

        [SerializeField] private CanvasSelection shapeSelection = new();
        [SerializeField] private CanvasInteractionState shapeInteraction = new();
        [SerializeField] private CanvasViewState shapeView = new();

        [SerializeField] private CanvasSelection profileSelection = new();
        [SerializeField] private CanvasInteractionState profileInteraction = new();
        [SerializeField] private CanvasViewState profileView = new();

        [SerializeField] private bool showMaterialSettings = true;
        [SerializeField] private List<Material> segmentMaterials = new();
        [SerializeField] private Material startCapMaterial;
        [SerializeField] private Material endCapMaterial;

        private EditorCanvas _shapeCanvas;
        private EditorCanvas _profileCanvas;

        private ICanvasToolPolicy _shapePolicy;
        private ICanvasToolPolicy _profilePolicy;

        private bool _isDraggingDivider;

        [MenuItem("Tools/DLN/Shape Stamper")]
        public static void Open()
        {
            ShapeStamperWindow window = GetWindow<ShapeStamperWindow>();
            window.titleContent = new GUIContent("Shape Stamper");
            window.minSize = new Vector2(760f, 520f);
            window.Show();
        }

        private void OnEnable()
        {
            shapeDocument ??= new ShapeCanvasDocument();
            profileDocument ??= new ProfileCanvasDocument();

            shapeSelection ??= new CanvasSelection();
            shapeInteraction ??= new CanvasInteractionState();
            shapeView ??= new CanvasViewState();

            profileSelection ??= new CanvasSelection();
            profileInteraction ??= new CanvasInteractionState();
            profileView ??= new CanvasViewState();

            segmentMaterials ??= new List<Material>();

            shapeDocument.EnsureValidShape();
            profileDocument.EnsureValidProfile();

            _shapePolicy ??= new ShapeCanvasPolicy(shapeDocument);
            _profilePolicy ??= new ProfileCanvasPolicy(profileDocument);

            _shapeCanvas = new EditorCanvas(shapeDocument, _shapePolicy, shapeSelection, shapeInteraction, shapeView);
            _profileCanvas = new EditorCanvas(profileDocument, _profilePolicy, profileSelection, profileInteraction, profileView);
        }

        private void OnGUI()
        {
            shapeDocument ??= new ShapeCanvasDocument();
            profileDocument ??= new ProfileCanvasDocument();
            segmentMaterials ??= new List<Material>();

            shapeDocument.EnsureValidShape();
            profileDocument.EnsureValidProfile();

            DrawTopBar();

            Rect fullCanvasArea = new Rect(
                0f,
                TopBarHeight,
                position.width,
                Mathf.Max(0f, position.height - TopBarHeight)
            );

            ComputeSplitRects(fullCanvasArea, out Rect leftPanelRect, out Rect dividerRect, out Rect dividerHitRect, out Rect rightPanelRect);
            HandleDividerInput(dividerHitRect);

            Rect leftAllowedRect = ShapeCanvasUtility.GetAllowedCanvasRect(leftPanelRect, canvasPercentOfWindow, CanvasPadding);
            Rect rightAllowedRect = ShapeCanvasUtility.GetAllowedCanvasRect(rightPanelRect, canvasPercentOfWindow, CanvasPadding);

            DrawPanelBackground(leftPanelRect);
            DrawPanelBackground(rightPanelRect);
            DrawDivider(dividerRect, dividerHitRect);

            _shapeCanvas.Draw(leftAllowedRect);
            _profileCanvas.Draw(rightAllowedRect);

            if (Event.current.type == EventType.Repaint)
                Repaint();
        }

        private void DrawTopBar()
        {
            EditorGUILayout.BeginVertical(GUILayout.Height(TopBarHeight));
            EditorGUILayout.Space(6f);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Shape World", GUILayout.Width(82f));
            EditorGUILayout.LabelField("W", GUILayout.Width(16f));
            float newShapeWidth = EditorGUILayout.FloatField(shapeDocument.WorldSizeMeters.x, GUILayout.Width(70f));
            EditorGUILayout.LabelField("H", GUILayout.Width(16f));
            float newShapeHeight = EditorGUILayout.FloatField(shapeDocument.WorldSizeMeters.y, GUILayout.Width(70f));

            GUILayout.Space(18f);

            EditorGUILayout.LabelField("Profile World", GUILayout.Width(84f));
            EditorGUILayout.LabelField("W", GUILayout.Width(16f));
            float newProfileWidth = EditorGUILayout.FloatField(profileDocument.WorldSizeMeters.x, GUILayout.Width(70f));
            EditorGUILayout.LabelField("H", GUILayout.Width(16f));
            float newProfileHeight = EditorGUILayout.FloatField(profileDocument.WorldSizeMeters.y, GUILayout.Width(70f));

            if (GUILayout.Button("Generate Profile", GUILayout.Width(120f)))
            {
                ShapeStamperProfileGenerator.Generate(
                    shapeDocument,
                    profileDocument,
                    BuildPreviewMaterialSettings());
            }

            if (GUILayout.Button("Bake", GUILayout.Width(100f)))
            {
                ShapeStamperBakeService.BakeShapeFaceToScene(shapeDocument);
            }

            GUILayout.FlexibleSpace();

            bool newHasInnerShape = EditorGUILayout.ToggleLeft("Inner Shape", shapeDocument.HasInnerShape, GUILayout.Width(100f));
            if (newHasInnerShape && !shapeDocument.HasInnerShape)
            {
                shapeDocument.HasInnerShape = true;
                shapeDocument.EnsureDefaultInnerShape();
                shapeDocument.EditMode = ShapeCanvasDocument.ShapeLoopEditMode.Inner;
                shapeSelection.Clear();
                shapeInteraction.Clear();
            }
            else if (!newHasInnerShape && shapeDocument.HasInnerShape)
            {
                shapeDocument.HasInnerShape = false;
                shapeDocument.EditMode = ShapeCanvasDocument.ShapeLoopEditMode.Outer;
                shapeSelection.Clear();
                shapeInteraction.Clear();
            }

            if (shapeDocument.HasInnerShape)
            {
                var newEditMode = (ShapeCanvasDocument.ShapeLoopEditMode)EditorGUILayout.EnumPopup(
                    shapeDocument.EditMode,
                    GUILayout.Width(90f));

                if (newEditMode != shapeDocument.EditMode)
                {
                    shapeDocument.EditMode = newEditMode;
                    shapeSelection.Clear();
                    shapeInteraction.Clear();
                }
            }

            if (GUILayout.Button("Reset Shape", GUILayout.Width(100f)))
            {
                shapeDocument.ResetToDefaultTriangle();
                shapeSelection.Clear();
                shapeInteraction.Clear();
                shapeView.ResetView();
                GUI.FocusControl(null);
            }

            if (GUILayout.Button("Reset Profile", GUILayout.Width(100f)))
            {
                profileDocument.ResetDefaultProfile();
                profileSelection.Clear();
                profileInteraction.Clear();
                profileView.ResetView();
                GUI.FocusControl(null);
            }

            EditorGUILayout.EndHorizontal();

            Vector2 requestedShapeSize = new Vector2(
                Mathf.Max(0.0001f, newShapeWidth),
                Mathf.Max(0.0001f, newShapeHeight)
            );

            if (!Mathf.Approximately(requestedShapeSize.x, shapeDocument.WorldSizeMeters.x) ||
                !Mathf.Approximately(requestedShapeSize.y, shapeDocument.WorldSizeMeters.y))
            {
                shapeDocument.ResizeWorld(requestedShapeSize);
            }

            Vector2 requestedProfileSize = new Vector2(
                Mathf.Max(0.0001f, newProfileWidth),
                Mathf.Max(0.0001f, newProfileHeight)
            );

            if (!Mathf.Approximately(requestedProfileSize.x, profileDocument.WorldSizeMeters.x) ||
                !Mathf.Approximately(requestedProfileSize.y, profileDocument.WorldSizeMeters.y))
            {
                profileDocument.ResizeWorld(requestedProfileSize);
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Canvas %", GUILayout.Width(82f));

            EditorGUILayout.LabelField("W", GUILayout.Width(16f));
            float newPercentWidth = EditorGUILayout.FloatField(canvasPercentOfWindow.x * 100f, GUILayout.Width(70f));

            EditorGUILayout.LabelField("H", GUILayout.Width(16f));
            float newPercentHeight = EditorGUILayout.FloatField(canvasPercentOfWindow.y * 100f, GUILayout.Width(70f));

            canvasPercentOfWindow = new Vector2(
                Mathf.Clamp(newPercentWidth, 5f, 100f) / 100f,
                Mathf.Clamp(newPercentHeight, 5f, 100f) / 100f
            );

            GUILayout.Space(18f);
            EditorGUILayout.LabelField("Split", GUILayout.Width(30f));
            verticalDividerPercent = GUILayout.HorizontalSlider(verticalDividerPercent, 0.2f, 0.8f, GUILayout.Width(140f));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Frame Shape", GUILayout.Width(100f)))
                shapeView.ResetView();

            if (GUILayout.Button("Frame Profile", GUILayout.Width(100f)))
                profileView.ResetView();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                $"Shape Points: {shapeDocument.PointCount}   Selected: {shapeSelection.Count}",
                EditorStyles.miniLabel
            );
            GUILayout.Space(20f);
            EditorGUILayout.LabelField(
                $"Profile Points: {profileDocument.Points.Count}   Selected: {profileSelection.Count}",
                EditorStyles.miniLabel
            );
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();
            DrawShapeSelectionInspector();
            GUILayout.Space(10f);
            DrawProfileSelectionInspector();
            EditorGUILayout.EndHorizontal();

            DrawMaterialSettings();

            EditorGUILayout.EndVertical();
        }

        private void DrawShapeSelectionInspector()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.MinHeight(88f));
            EditorGUILayout.LabelField("Shape Selection", EditorStyles.boldLabel);
            DrawPointInspector(shapeDocument, shapeSelection);
            EditorGUILayout.EndVertical();
        }

        private void DrawProfileSelectionInspector()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.MinHeight(88f));
            EditorGUILayout.LabelField("Profile Selection", EditorStyles.boldLabel);
            DrawPointInspector(profileDocument, profileSelection);
            EditorGUILayout.EndVertical();
        }

        private void DrawPointInspector(ShapeCanvasDocument document, CanvasSelection selection)
        {
            if (!TryGetSingleSelectedPoint(selection, out int pointId))
            {
                EditorGUILayout.LabelField("Select one point to edit anchors and offsets.", EditorStyles.miniLabel);
                return;
            }

            if (!document.TryGetPointById(pointId, out CanvasPoint point))
            {
                EditorGUILayout.LabelField("Selected point not found.", EditorStyles.miniLabel);
                return;
            }

            DrawPointFields(document.GetCanvasFrameRect(), document.SetPoint, point);
        }

        private void DrawPointInspector(ProfileCanvasDocument document, CanvasSelection selection)
        {
            if (!TryGetSingleSelectedPoint(selection, out int pointId))
            {
                EditorGUILayout.LabelField("Select one point to edit anchors and offsets.", EditorStyles.miniLabel);
                return;
            }

            if (!document.TryGetPointById(pointId, out CanvasPoint point))
            {
                EditorGUILayout.LabelField("Selected point not found.", EditorStyles.miniLabel);
                return;
            }

            DrawPointFields(document.GetCanvasFrameRect(), document.SetPoint, point);
        }

        private void DrawPointFields(Rect bounds, Func<CanvasPoint, bool> applyPoint, CanvasPoint point)
        {
            EditorGUILayout.LabelField($"Point {point.Id}", EditorStyles.miniBoldLabel);

            CanvasAnchorX newXAnchor = (CanvasAnchorX)EditorGUILayout.EnumPopup("X Anchor", point.XAnchor);
            CanvasAnchorY newYAnchor = (CanvasAnchorY)EditorGUILayout.EnumPopup("Y Anchor", point.YAnchor);

            if (newXAnchor != point.XAnchor || newYAnchor != point.YAnchor)
            {
                ShapeCanvasConstraintUtility.AssignAnchorsFromCurrentPosition(ref point, bounds, newXAnchor, newYAnchor);
                applyPoint(point);
            }

            EditorGUI.BeginChangeCheck();

            using (new EditorGUI.DisabledScope(point.XAnchor == CanvasAnchorX.None))
            {
                point.OffsetX = EditorGUILayout.FloatField("Offset X", point.OffsetX);
            }

            using (new EditorGUI.DisabledScope(point.YAnchor == CanvasAnchorY.None))
            {
                point.OffsetY = EditorGUILayout.FloatField("Offset Y", point.OffsetY);
            }

            if (EditorGUI.EndChangeCheck())
            {
                ShapeCanvasConstraintUtility.ResolvePositionFromOffsets(ref point, bounds);
                ShapeCanvasConstraintUtility.ClampPointToBounds(ref point, bounds);
                applyPoint(point);
            }

            if (GUILayout.Button("Recompute Offsets From Position", GUILayout.Width(210f)))
            {
                ShapeCanvasConstraintUtility.RecalculateOffsetsFromPosition(ref point, bounds);
                applyPoint(point);
            }
        }

        private static bool TryGetSingleSelectedPoint(CanvasSelection selection, out int pointId)
        {
            pointId = -1;
            int pointCount = 0;

            foreach (CanvasElementRef element in selection.Elements)
            {
                if (element.Type != CanvasElementType.Point)
                    continue;

                pointCount++;
                pointId = element.Id;

                if (pointCount > 1)
                    return false;
            }

            return pointCount == 1;
        }

        private void DrawMaterialSettings()
        {
            EditorGUILayout.Space(6f);
            showMaterialSettings = EditorGUILayout.Foldout(showMaterialSettings, "Materials", true);

            if (!showMaterialSettings)
                return;

            EditorGUI.indentLevel++;

            int requiredSegments = Mathf.Max(0, profileDocument.Points.Count - 1);
            EditorGUILayout.LabelField(
                $"Profile segments currently needed: {requiredSegments}",
                EditorStyles.miniLabel);

            int newSize = EditorGUILayout.IntField("Segment Slots", segmentMaterials.Count);
            newSize = Mathf.Max(0, newSize);

            while (segmentMaterials.Count < newSize)
                segmentMaterials.Add(null);

            while (segmentMaterials.Count > newSize)
                segmentMaterials.RemoveAt(segmentMaterials.Count - 1);

            for (int i = 0; i < segmentMaterials.Count; i++)
            {
                segmentMaterials[i] = (Material)EditorGUILayout.ObjectField(
                    $"Segment {i}",
                    segmentMaterials[i],
                    typeof(Material),
                    false);
            }

            startCapMaterial = (Material)EditorGUILayout.ObjectField(
                "Start Cap",
                startCapMaterial,
                typeof(Material),
                false);

            endCapMaterial = (Material)EditorGUILayout.ObjectField(
                "End Cap",
                endCapMaterial,
                typeof(Material),
                false);

            EditorGUI.indentLevel--;
        }

        private ShapeStampPreviewMaterialSettings BuildPreviewMaterialSettings()
        {
            ShapeStampPreviewMaterialSettings settings = new ShapeStampPreviewMaterialSettings();
            settings.StartCapMaterial = startCapMaterial;
            settings.EndCapMaterial = endCapMaterial;

            if (segmentMaterials != null)
            {
                for (int i = 0; i < segmentMaterials.Count; i++)
                    settings.SegmentMaterials.Add(segmentMaterials[i]);
            }

            return settings;
        }

        private void ComputeSplitRects(
            Rect fullCanvasArea,
            out Rect leftPanelRect,
            out Rect dividerRect,
            out Rect dividerHitRect,
            out Rect rightPanelRect)
        {
            float clampedPercent = Mathf.Clamp(verticalDividerPercent, 0.2f, 0.8f);

            float totalWidth = fullCanvasArea.width;
            float availableWidth = Mathf.Max(0f, totalWidth - DividerWidth);

            float leftWidth = Mathf.Round(availableWidth * clampedPercent);
            float rightWidth = availableWidth - leftWidth;

            if (leftWidth < MinPanelWidth)
            {
                leftWidth = MinPanelWidth;
                rightWidth = availableWidth - leftWidth;
            }

            if (rightWidth < MinPanelWidth)
            {
                rightWidth = MinPanelWidth;
                leftWidth = availableWidth - rightWidth;
            }

            float dividerX = fullCanvasArea.x + leftWidth;

            leftPanelRect = new Rect(fullCanvasArea.x, fullCanvasArea.y, Mathf.Max(0f, leftWidth), fullCanvasArea.height);
            dividerRect = new Rect(dividerX, fullCanvasArea.y, DividerWidth, fullCanvasArea.height);
            dividerHitRect = new Rect(dividerRect.center.x - DividerHitWidth * 0.5f, fullCanvasArea.y, DividerHitWidth, fullCanvasArea.height);
            rightPanelRect = new Rect(dividerRect.xMax, fullCanvasArea.y, Mathf.Max(0f, rightWidth), fullCanvasArea.height);
        }

        private void HandleDividerInput(Rect dividerHitRect)
        {
            Event evt = Event.current;
            EditorGUIUtility.AddCursorRect(dividerHitRect, MouseCursor.ResizeHorizontal);

            switch (evt.type)
            {
                case EventType.MouseDown:
                    if (evt.button == 0 && dividerHitRect.Contains(evt.mousePosition))
                    {
                        _isDraggingDivider = true;
                        evt.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (_isDraggingDivider && evt.button == 0)
                    {
                        float usableWidth = Mathf.Max(1f, position.width - DividerWidth);
                        verticalDividerPercent = Mathf.Clamp(evt.mousePosition.x / usableWidth, 0.2f, 0.8f);
                        evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (_isDraggingDivider && evt.button == 0)
                    {
                        _isDraggingDivider = false;
                        evt.Use();
                    }
                    break;
            }
        }

        private void DrawPanelBackground(Rect panelRect)
        {
            EditorGUI.DrawRect(panelRect, new Color(0.10f, 0.10f, 0.10f));
        }

        private void DrawDivider(Rect dividerRect, Rect dividerHitRect)
        {
            Color dividerColor = _isDraggingDivider
                ? new Color(0.55f, 0.55f, 0.55f)
                : (dividerHitRect.Contains(Event.current.mousePosition)
                    ? new Color(0.42f, 0.42f, 0.42f)
                    : new Color(0.28f, 0.28f, 0.28f));

            EditorGUI.DrawRect(dividerRect, dividerColor);
        }
    }
}
""",
}

def write_text(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8", newline="\n")
    print(f"[write] {path}")

def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("repo_root", help="Path to the GeomNodes repo root")
    args = parser.parse_args()

    root = Path(args.repo_root).expanduser().resolve()
    if not root.exists():
        print(f"Repo root does not exist: {root}", file=sys.stderr)
        return 1

    for rel_path, content in FILES.items():
        write_text(root / rel_path, content)

    print("\nDone.")
    print("Patched files:", len(FILES))
    print("Includes namespace cleanup, resize plumbing, drag offset sync, and point inspectors.")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
'''
