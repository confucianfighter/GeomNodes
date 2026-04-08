#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    public sealed class ProfileCanvasPolicy : ICanvasToolPolicy
    {
        private readonly ProfileCanvasDocument _document;

        public ProfileCanvasPolicy(ProfileCanvasDocument document)
        {
            _document = document;
        }

        public void DrawOverlay(EditorCanvas canvas, Rect canvasRect)
        {
            if (_document == null)
                return;

            Rect labelRect = new Rect(canvasRect.x + 8f, canvasRect.y + 8f, 220f, 20f);
            GUI.Label(
                labelRect,
                $"Profile  {_document.WorldSizeMeters.x:0.###}m x {_document.WorldSizeMeters.y:0.###}m",
                EditorStyles.miniLabel
            );

            Handles.BeginGUI();

            Color old = Handles.color;
            Handles.color = new Color(1f, 1f, 1f, 0.12f);

            Vector2 origin = CanvasMath.CanvasToScreen(Vector2.zero, canvasRect, canvas.View);
            Vector2 xAxis = CanvasMath.CanvasToScreen(new Vector2(_document.WorldSizeMeters.x, 0f), canvasRect, canvas.View);
            Vector2 yAxis = CanvasMath.CanvasToScreen(new Vector2(0f, _document.WorldSizeMeters.y), canvasRect, canvas.View);

            Handles.DrawLine(origin, xAxis);
            Handles.DrawLine(origin, yAxis);

            Handles.color = old;
            Handles.EndGUI();
        }

        public void OnMouseDown(EditorCanvas canvas, Event evt) { }
        public void OnDrag(EditorCanvas canvas, Event evt) { }
        public void OnClick(EditorCanvas canvas, Event evt) { }

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

            int newPointId = GetNextPointId(_document);
            canvasPos = ClampToProfileBounds(canvasPos);

            _document.Points.Add(new CanvasPoint
            {
                Id = newPointId,
                Position = canvasPos
            });

            _document.MarkDirty();

            canvas.Selection.Clear();
            canvas.Selection.Add(CanvasElementRef.ForPoint(newPointId));
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
            splitPoint = ClampToProfileBounds(splitPoint);

            int newPointId = GetNextPointId(_document);
            int newEdgeIdA = GetNextEdgeId(_document);
            int newEdgeIdB = newEdgeIdA + 1;

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
                B = edge.B
            });
            _document.Edges.Insert(edgeIndex, new CanvasEdge
            {
                Id = newEdgeIdA,
                A = edge.A,
                B = newPointId
            });

            RemapOffsetsAfterSplit(edge.Id, newEdgeIdA);

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
                    case CanvasElementType.Point: pointIdsToDelete.Add(element.Id); break;
                    case CanvasElementType.Edge: edgeIdsToDelete.Add(element.Id); break;
                    case CanvasElementType.Offset: offsetIdsToDelete.Add(element.Id); break;
                }
            }

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
            position = ClampToProfileBounds(position);
        }

        private Vector2 ClampToProfileBounds(Vector2 p)
        {
            return new Vector2(
                Mathf.Clamp(p.x, 0f, _document.WorldSizeMeters.x),
                Mathf.Clamp(p.y, 0f, _document.WorldSizeMeters.y)
            );
        }

        private static int GetNextPointId(ICanvasDocument document)
        {
            int maxId = -1;
            foreach (CanvasPoint p in document.Points)
                maxId = Mathf.Max(maxId, p.Id);
            return maxId + 1;
        }

        private static int GetNextEdgeId(ICanvasDocument document)
        {
            int maxId = -1;
            foreach (CanvasEdge e in document.Edges)
                maxId = Mathf.Max(maxId, e.Id);
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

        private void RemapOffsetsAfterSplit(int oldEdgeId, int replacementEdgeId)
        {
            for (int i = 0; i < _document.Offsets.Count; i++)
            {
                CanvasOffsetConstraint offset = _document.Offsets[i];
                if (offset.EdgeId != oldEdgeId)
                    continue;

                offset.EdgeId = replacementEdgeId;
                _document.Offsets[i] = offset;
            }
        }
    }
}
#endif