#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    public sealed class ShapeCanvasPolicy : ICanvasToolPolicy
    {
        private readonly ShapeCanvasDocument _document;

        public ShapeCanvasPolicy(ShapeCanvasDocument document)
        {
            _document = document;
        }

        public void DrawOverlay(EditorCanvas canvas, Rect canvasRect)
        {
            if (_document == null)
                return;

            Handles.BeginGUI();

            Color old = Handles.color;
            Handles.color = new Color(1f, 1f, 1f, 0.18f);

            Vector2 originScreen = CanvasMath.CanvasToScreen(Vector2.zero, canvasRect, canvas.View, _document);
            Vector2 xScreen = CanvasMath.CanvasToScreen(new Vector2(_document.WorldSizeMeters.x, 0f), canvasRect, canvas.View, _document);
            Vector2 yScreen = CanvasMath.CanvasToScreen(new Vector2(0f, _document.WorldSizeMeters.y), canvasRect, canvas.View, _document);

            Handles.DrawLine(originScreen, xScreen);
            Handles.DrawLine(originScreen, yScreen);

            Handles.color = old;
            Handles.EndGUI();

            DrawInactiveLoop(canvas);

            Rect labelRect = new Rect(canvasRect.x + 8f, canvasRect.y + 8f, 320f, 20f);
            string modeLabel = _document.HasInnerShape
                ? $"Mode: {_document.EditMode}"
                : "Mode: Outer";

            GUI.Label(
                labelRect,
                $"Shape  {_document.WorldSizeMeters.x:0.###}m x {_document.WorldSizeMeters.y:0.###}m   {modeLabel}",
                EditorStyles.miniLabel
            );
        }

        private void DrawInactiveLoop(EditorCanvas canvas)
        {
            if (_document == null || !_document.HasInnerShape)
                return;

            IList<CanvasPoint> points;
            IList<CanvasEdge> edges;
            Color edgeColor;
            Color pointColor;

            if (_document.EditMode == ShapeCanvasDocument.ShapeLoopEditMode.Inner)
            {
                points = _document.OuterPoints;
                edges = _document.OuterEdges;
                edgeColor = new Color(0.45f, 0.85f, 0.45f, 0.55f);
                pointColor = new Color(0.45f, 0.85f, 0.45f, 0.85f);
            }
            else
            {
                points = _document.InnerPoints;
                edges = _document.InnerEdges;
                edgeColor = new Color(1f, 0.6f, 0.2f, 0.85f);
                pointColor = new Color(1f, 0.5f, 0.15f, 1f);
            }

            Handles.BeginGUI();
            Color old = Handles.color;

            for (int i = 0; i < edges.Count; i++)
            {
                CanvasEdge edge = edges[i];
                if (!TryGetEdgeScreenPositions(canvas, points, edge, out Vector2 a, out Vector2 b))
                    continue;

                Handles.color = edgeColor;
                Handles.DrawAAPolyLine(2f, a, b);
            }

            for (int i = 0; i < points.Count; i++)
            {
                Vector2 screen = canvas.CanvasToScreen(points[i].Position);

                Handles.color = pointColor;
                Handles.DrawSolidDisc(screen, Vector3.forward, 5f);

                Handles.color = Color.black;
                Handles.DrawWireDisc(screen, Vector3.forward, 6f);
            }

            Handles.color = old;
            Handles.EndGUI();
        }

        private static bool TryGetEdgeScreenPositions(EditorCanvas canvas, IList<CanvasPoint> points, CanvasEdge edge, out Vector2 a, out Vector2 b)
        {
            a = default;
            b = default;

            if (!TryGetPoint(points, edge.A, out CanvasPoint pointA))
                return false;

            if (!TryGetPoint(points, edge.B, out CanvasPoint pointB))
                return false;

            a = canvas.CanvasToScreen(pointA.Position);
            b = canvas.CanvasToScreen(pointB.Position);
            return true;
        }

        private static bool TryGetPoint(IList<CanvasPoint> points, int pointId, out CanvasPoint point)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].Id == pointId)
                {
                    point = points[i];
                    return true;
                }
            }

            point = default;
            return false;
        }

        public void OnMouseDown(EditorCanvas canvas, Event evt)
        {
        }

        public void OnDrag(EditorCanvas canvas, Event evt)
        {
        }

        public void OnClick(EditorCanvas canvas, Event evt)
        {
        }

        public void OnKeyDown(EditorCanvas canvas, Event evt)
        {
            if (_document == null)
                return;

            if (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace)
            {
                DeleteSelection(canvas);
                evt.Use();
            }
        }

        public void AddPointAtCanvasPosition(EditorCanvas canvas, Vector2 canvasPos)
        {
            if (_document == null)
                return;

            Vector2 clamped = ClampToShapeBounds(canvasPos);

            int newId = GetNextPointId(_document);
            _document.Points.Add(new CanvasPoint
            {
                Id = newId,
                Position = clamped
            });

            _document.MarkDirty();

            canvas.Selection.Clear();
            canvas.Selection.Add(CanvasElementRef.ForPoint(newId));
        }

        public void SplitEdgeAtScreenPosition(EditorCanvas canvas, CanvasElementRef edgeRef, Vector2 screenPos)
        {
            if (_document == null || !edgeRef.IsEdge)
                return;

            if (!TryGetEdgeById(_document, edgeRef.Id, out CanvasEdge edge))
                return;

            if (!CanvasMath.TryGetEdgeCanvasPositions(edge, _document, out Vector2 a, out Vector2 b))
                return;

            Vector2 mouseCanvas = canvas.ScreenToCanvas(screenPos);
            Vector2 splitPoint = CanvasMath.ClosestPointOnSegment(mouseCanvas, a, b);
            splitPoint = ClampToShapeBounds(splitPoint);

            int newPointId = GetNextPointId(_document);
            int newEdgeIdA = GetNextEdgeId(_document);
            int newEdgeIdB = newEdgeIdA + 1;
            float inheritedProfileXScale = Mathf.Max(0.0001f, edge.ProfileXScale);

            int edgeIndex = GetEdgeIndexById(_document, edge.Id);
            if (edgeIndex < 0)
                return;

            _document.Points.Add(new CanvasPoint
            {
                Id = newPointId,
                Position = splitPoint
            });

            _document.Edges.RemoveAt(edgeIndex);
            _document.Edges.Insert(edgeIndex, new CanvasEdge
            {
                Id = newEdgeIdB,
                A = newPointId,
                B = edge.B,
                ProfileXScale = inheritedProfileXScale
            });
            _document.Edges.Insert(edgeIndex, new CanvasEdge
            {
                Id = newEdgeIdA,
                A = edge.A,
                B = newPointId,
                ProfileXScale = inheritedProfileXScale
            });

            RemapOffsetsAfterSplit(edge.Id, newEdgeIdA, newEdgeIdB);

            _document.MarkDirty();

            canvas.Selection.Clear();
            canvas.Selection.Add(CanvasElementRef.ForPoint(newPointId));
        }

        public void DeleteSelection(EditorCanvas canvas)
        {
            if (_document == null)
                return;

            List<int> pointIdsToDelete = new();
            HashSet<int> edgeIdsToDelete = new();
            HashSet<int> offsetIdsToDelete = new();

            foreach (CanvasElementRef element in canvas.Selection.Elements)
            {
                switch (element.Type)
                {
                    case CanvasElementType.Point:
                        pointIdsToDelete.Add(element.Id);
                        break;

                    case CanvasElementType.Edge:
                        edgeIdsToDelete.Add(element.Id);
                        break;

                    case CanvasElementType.Offset:
                        offsetIdsToDelete.Add(element.Id);
                        break;
                }
            }

            if (_document.Points.Count - pointIdsToDelete.Count < 3)
                pointIdsToDelete.Clear();

            if (pointIdsToDelete.Count > 0)
                DeletePointsAndConnectedData(_document, pointIdsToDelete);

            if (edgeIdsToDelete.Count > 0)
            {
                RemoveEdgesById(_document, edgeIdsToDelete);
                RemoveOffsetsByEdgeIds(_document, edgeIdsToDelete);
            }

            if (offsetIdsToDelete.Count > 0)
                RemoveOffsetsById(_document, offsetIdsToDelete);

            _document.MarkDirty();
            canvas.Selection.Clear();
            canvas.Interaction.Clear();
        }

        public void ConstrainDraggedPoint(EditorCanvas canvas, int pointId, ref Vector2 position)
        {
            position = ClampToShapeBounds(position);
        }

        private Vector2 ClampToShapeBounds(Vector2 p)
        {
            if (_document == null)
                return p;

            return new Vector2(
                Mathf.Clamp(p.x, 0f, _document.WorldSizeMeters.x),
                Mathf.Clamp(p.y, 0f, _document.WorldSizeMeters.y)
            );
        }

        private static int GetNextPointId(ShapeCanvasDocument document)
        {
            int maxId = -1;

            for (int i = 0; i < document.OuterPoints.Count; i++)
                maxId = Mathf.Max(maxId, document.OuterPoints[i].Id);

            for (int i = 0; i < document.InnerPoints.Count; i++)
                maxId = Mathf.Max(maxId, document.InnerPoints[i].Id);

            return maxId + 1;
        }

        private static int GetNextEdgeId(ShapeCanvasDocument document)
        {
            int maxId = -1;

            for (int i = 0; i < document.OuterEdges.Count; i++)
                maxId = Mathf.Max(maxId, document.OuterEdges[i].Id);

            for (int i = 0; i < document.InnerEdges.Count; i++)
                maxId = Mathf.Max(maxId, document.InnerEdges[i].Id);

            return maxId + 1;
        }

        private static bool TryGetEdgeById(ICanvasDocument document, int edgeId, out CanvasEdge edge)
        {
            foreach (CanvasEdge e in document.Edges)
            {
                if (e.Id == edgeId)
                {
                    edge = e;
                    return true;
                }
            }

            edge = default;
            return false;
        }

        private static int GetEdgeIndexById(ICanvasDocument document, int edgeId)
        {
            for (int i = 0; i < document.Edges.Count; i++)
            {
                if (document.Edges[i].Id == edgeId)
                    return i;
            }

            return -1;
        }

        private static void DeletePointsAndConnectedData(ICanvasDocument document, List<int> pointIds)
        {
            HashSet<int> pointSet = new(pointIds);
            HashSet<int> deletedEdgeIds = new();

            for (int i = document.Edges.Count - 1; i >= 0; i--)
            {
                CanvasEdge edge = document.Edges[i];
                if (pointSet.Contains(edge.A) || pointSet.Contains(edge.B))
                {
                    deletedEdgeIds.Add(edge.Id);
                    document.Edges.RemoveAt(i);
                }
            }

            for (int i = document.Offsets.Count - 1; i >= 0; i--)
            {
                if (deletedEdgeIds.Contains(document.Offsets[i].EdgeId))
                    document.Offsets.RemoveAt(i);
            }

            for (int i = document.Points.Count - 1; i >= 0; i--)
            {
                if (pointSet.Contains(document.Points[i].Id))
                    document.Points.RemoveAt(i);
            }
        }

        private static void RemoveEdgesById(ICanvasDocument document, HashSet<int> edgeIds)
        {
            for (int i = document.Edges.Count - 1; i >= 0; i--)
            {
                if (edgeIds.Contains(document.Edges[i].Id))
                    document.Edges.RemoveAt(i);
            }
        }

        private static void RemoveOffsetsByEdgeIds(ICanvasDocument document, HashSet<int> edgeIds)
        {
            for (int i = document.Offsets.Count - 1; i >= 0; i--)
            {
                if (edgeIds.Contains(document.Offsets[i].EdgeId))
                    document.Offsets.RemoveAt(i);
            }
        }

        private static void RemoveOffsetsById(ICanvasDocument document, HashSet<int> offsetIds)
        {
            for (int i = document.Offsets.Count - 1; i >= 0; i--)
            {
                if (offsetIds.Contains(document.Offsets[i].Id))
                    document.Offsets.RemoveAt(i);
            }
        }

        private void RemapOffsetsAfterSplit(int oldEdgeId, int newEdgeIdA, int newEdgeIdB)
        {
            if (_document.IsEditingInnerLoop)
                return;

            for (int i = 0; i < _document.Offsets.Count; i++)
            {
                CanvasOffsetConstraint offset = _document.Offsets[i];
                if (offset.EdgeId != oldEdgeId)
                    continue;

                offset.EdgeId = newEdgeIdA;
                _document.Offsets[i] = offset;
            }
        }
    }
}
#endif
