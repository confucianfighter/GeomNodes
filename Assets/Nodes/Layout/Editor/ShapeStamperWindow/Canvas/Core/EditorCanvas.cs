#if UNITY_EDITOR
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
            DrawMarquee(canvasRect);

            Policy?.DrawOverlay(this, canvasRect);

            if (evt.type == EventType.Repaint)
                EditorGUIUtility.AddCursorRect(canvasRect, MouseCursor.Arrow);
        }

        public Vector2 ScreenToCanvas(Vector2 screenPosition)
        {
            return CanvasMath.ScreenToCanvas(screenPosition, LastCanvasRect, View, Document);
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
                menu.AddItem(new GUIContent("Add Point"), false, () =>
                {
                    Policy?.AddPointAtCanvasPosition(this, ScreenToCanvas(evt.mousePosition));
                });

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
            Rect worldRect = CanvasMath.GetWorldRect(Document);
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
            for (int i = 0; i < Document.Points.Count; i++)
            {
                if (Document.Points[i].Id != pointId)
                    continue;

                CanvasPoint p = Document.Points[i];
                p.Position = position;
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