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

            CanvasGuideDrawing.DrawProfileGuides(canvas, canvasRect, _document);

            Rect labelRect = new Rect(canvasRect.x + 8f, canvasRect.y + 8f, 260f, 20f);
            GUI.Label(
                labelRect,
                $"Profile  {_document.WorldSizeMeters.x:0.###}m x {_document.WorldSizeMeters.y:0.###}m",
                EditorStyles.miniLabel
            );
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

            int newPointId = _document.GetNextPointId();

            Vector2 newPos;
            if (_document.Points.Count == 0)
            {
                newPos = ClampToProfileBounds(canvasPos);
            }
            else if (_document.Points.Count == 1)
            {
                CanvasPoint last = _document.Points[_document.Points.Count - 1];
                newPos = ClampToProfileBounds(last.Position + new Vector2(0.15f, 0f));
            }
            else
            {
                CanvasPoint prev = _document.Points[_document.Points.Count - 2];
                CanvasPoint last = _document.Points[_document.Points.Count - 1];

                Vector2 dir = last.Position - prev.Position;
                if (dir.sqrMagnitude < 0.000001f)
                    dir = Vector2.right;
                else
                    dir.Normalize();

                float defaultLength = Mathf.Max(_document.WorldSizeMeters.x, _document.WorldSizeMeters.y) * 0.15f;
                newPos = ClampToProfileBounds(last.Position + dir * defaultLength);
            }

            ProfilePoint pp = new ProfilePoint
            {
                Id = newPointId,
                Position = newPos,
                YAnchor = CanvasAnchorY.Floating,
                XSpan = ProfileXSpan.PaddingToContent,
                ZSpan = ProfileZSpan.MainDepth,
                XT = 0f,
                ZT = 0.5f
            };

            ProfileCanvasPointResolver.SetSpansFromPosition(
                ref pp,
                _document.GetCanvasFrameRect(),
                _document.PaddingGuideX,
                _document.BorderGuideX);

            _document.ProfilePoints.Add(pp);
            _document.RebuildOpenEdges();
            _document.SyncDisplayPointsFromProfilePoints();
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
            Vector2 splitPoint = ClampToProfileBounds(CanvasMath.ClosestPointOnSegment(mouseCanvas, a, b));

            int newPointId = _document.GetNextPointId();
            int edgeIndex = GetEdgeIndexById(_document, edge.Id);
            if (edgeIndex < 0)
                return;

            ProfilePoint pp = new ProfilePoint
            {
                Id = newPointId,
                Position = splitPoint,
                YAnchor = CanvasAnchorY.Floating,
                XSpan = ProfileXSpan.PaddingToContent,
                ZSpan = ProfileZSpan.MainDepth,
                XT = 0f,
                ZT = 0.5f
            };

            ProfileCanvasPointResolver.SetSpansFromPosition(
                ref pp,
                _document.GetCanvasFrameRect(),
                _document.PaddingGuideX,
                _document.BorderGuideX);

            int insertIndex = edgeIndex + 1;
            _document.ProfilePoints.Insert(insertIndex, pp);

            _document.RebuildOpenEdges();
            RemapOffsetsAfterSplit(edge.Id, edgeIndex);
            _document.SyncDisplayPointsFromProfilePoints();
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
                DeletePointsAndConnectedData(pointIdsToDelete);

            if (edgeIdsToDelete.Count > 0)
            {
                RemoveEdgesById(edgeIdsToDelete);
                RemoveOffsetsByEdgeIds(edgeIdsToDelete);
            }

            if (offsetIdsToDelete.Count > 0)
                RemoveOffsetsById(offsetIdsToDelete);

            _document.RebuildOpenEdges();
            _document.SyncDisplayPointsFromProfilePoints();
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

        private void DeletePointsAndConnectedData(List<int> pointIds)
        {
            HashSet<int> pointSet = new(pointIds);
            HashSet<int> deletedEdgeIds = new();

            for (int i = _document.Edges.Count - 1; i >= 0; i--)
            {
                CanvasEdge edge = _document.Edges[i];
                if (pointSet.Contains(edge.A) || pointSet.Contains(edge.B))
                {
                    deletedEdgeIds.Add(edge.Id);
                    _document.Edges.RemoveAt(i);
                }
            }

            for (int i = _document.Offsets.Count - 1; i >= 0; i--)
            {
                if (deletedEdgeIds.Contains(_document.Offsets[i].EdgeId))
                    _document.Offsets.RemoveAt(i);
            }

            for (int i = _document.ProfilePoints.Count - 1; i >= 0; i--)
            {
                if (pointSet.Contains(_document.ProfilePoints[i].Id))
                    _document.ProfilePoints.RemoveAt(i);
            }
        }

        private void RemoveEdgesById(HashSet<int> edgeIds)
        {
            for (int i = _document.Edges.Count - 1; i >= 0; i--)
            {
                if (edgeIds.Contains(_document.Edges[i].Id))
                    _document.Edges.RemoveAt(i);
            }
        }

        private void RemoveOffsetsByEdgeIds(HashSet<int> edgeIds)
        {
            for (int i = _document.Offsets.Count - 1; i >= 0; i--)
            {
                if (edgeIds.Contains(_document.Offsets[i].EdgeId))
                    _document.Offsets.RemoveAt(i);
            }
        }

        private void RemoveOffsetsById(HashSet<int> offsetIds)
        {
            for (int i = _document.Offsets.Count - 1; i >= 0; i--)
            {
                if (offsetIds.Contains(_document.Offsets[i].Id))
                    _document.Offsets.RemoveAt(i);
            }
        }

        private void RemapOffsetsAfterSplit(int oldEdgeId, int replacementEdgeIndex)
        {
            if (replacementEdgeIndex < 0 || replacementEdgeIndex >= _document.Edges.Count)
                return;

            int replacementEdgeId = _document.Edges[replacementEdgeIndex].Id;

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
