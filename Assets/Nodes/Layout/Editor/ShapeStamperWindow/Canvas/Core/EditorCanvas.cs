#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DLN
{
    /// <summary>
    /// Shared editor canvas core for shape/profile style 2D editing.
    /// This class should contain only generic canvas behavior:
    /// view transforms, hit testing, hover/selection, dragging, marquee, etc.
    /// Document-specific rules should live in ICanvasToolPolicy / document classes.
    /// </summary>
    [Serializable]
    public class EditorCanvas
    {
        private const float DefaultPointRadius = 6f;
        private const float DefaultEdgeHitDistance = 8f;
        private const float DefaultOffsetHitDistance = 10f;
        private const float DragStartThreshold = 4f;
        private const float GridSpacing = 20f;

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

            var evt = Event.current;

            HandleEvent(evt, canvasRect);

            DrawBackground(canvasRect);
            DrawGrid(canvasRect);

            CanvasDrawing.DrawDocument(
                canvasRect: canvasRect,
                document: Document,
                selection: Selection,
                interaction: Interaction,
                view: View);

            DrawMarquee(canvasRect);

            Policy?.DrawOverlay(this, canvasRect);

            if (evt.type == EventType.Repaint)
            {
                EditorGUIUtility.AddCursorRect(canvasRect, MouseCursor.Arrow);
            }
        }

        public Vector2 ScreenToCanvas(Vector2 screenPosition)
        {
            return CanvasMath.ScreenToCanvas(screenPosition, LastCanvasRect, View);
        }

        public Vector2 CanvasToScreen(Vector2 canvasPosition)
        {
            return CanvasMath.CanvasToScreen(canvasPosition, LastCanvasRect, View);
        }

        public void FrameAll(float padding = 30f)
        {
            if (Document == null || Document.Points == null || Document.Points.Count == 0)
                return;

            Rect bounds = GetCanvasBounds(Document.Points);
            View.FrameRect(bounds, LastCanvasRect.size, padding);
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
            Interaction.IsPanning = false;
            Interaction.IsMarqueeSelecting = false;
        }

        private void HandleEvent(Event evt, Rect canvasRect)
        {
            if (!canvasRect.Contains(evt.mousePosition) &&
                evt.type != EventType.MouseUp &&
                evt.type != EventType.MouseDrag &&
                evt.type != EventType.ScrollWheel)
            {
                return;
            }

            UpdateHover(evt.mousePosition);

            switch (evt.type)
            {
                case EventType.MouseDown:
                    HandleMouseDown(evt, canvasRect);
                    break;

                case EventType.MouseDrag:
                    HandleMouseDrag(evt, canvasRect);
                    break;

                case EventType.MouseUp:
                    HandleMouseUp(evt, canvasRect);
                    break;

                case EventType.ScrollWheel:
                    HandleScrollWheel(evt, canvasRect);
                    break;

                case EventType.KeyDown:
                    HandleKeyDown(evt);
                    break;

                case EventType.Repaint:
                    break;
            }
        }

        private void HandleMouseDown(Event evt, Rect canvasRect)
        {
            GUI.FocusControl(null);

            if (evt.button == 2 || (evt.button == 0 && evt.alt))
            {
                BeginPan(evt.mousePosition);
                evt.Use();
                return;
            }

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
                {
                    BeginPointDrag();
                }

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

        private void HandleMouseDrag(Event evt, Rect canvasRect)
        {
            if (Interaction.IsPanning)
            {
                Vector2 delta = evt.mousePosition - Interaction.LastMouseScreen;
                View.Pan += delta;
                Interaction.LastMouseScreen = evt.mousePosition;
                evt.Use();
                return;
            }

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

        private void HandleMouseUp(Event evt, Rect canvasRect)
        {
            if (Interaction.IsPanning)
            {
                EndPan();
                evt.Use();
                return;
            }

            if (evt.button == 0)
            {
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
        }

        private void HandleScrollWheel(Event evt, Rect canvasRect)
        {
            float zoomDelta = -evt.delta.y * 0.03f;
            Vector2 mouseCanvasBefore = ScreenToCanvas(evt.mousePosition);

            View.Zoom = Mathf.Clamp(View.Zoom * (1f + zoomDelta), 0.1f, 10f);

            Vector2 mouseScreenAfter = CanvasToScreen(mouseCanvasBefore);
            Vector2 screenDelta = evt.mousePosition - mouseScreenAfter;
            View.Pan += screenDelta;

            evt.Use();
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
                FrameAll();
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
            var pointHit = FindPointHit(mouseScreen);
            if (pointHit.IsValid)
                return pointHit;

            var edgeHit = FindEdgeHit(mouseScreen);
            if (edgeHit.IsValid)
                return edgeHit;

            var offsetHit = FindOffsetHit(mouseScreen);
            if (offsetHit.IsValid)
                return offsetHit;

            return default;
        }

        private CanvasElementRef FindPointHit(Vector2 mouseScreen)
        {
            float bestDistance = DefaultPointRadius + 4f;
            CanvasElementRef best = default;

            foreach (var point in Document.Points)
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

            foreach (var edge in Document.Edges)
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

            foreach (var offset in Document.Offsets)
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

        private void BeginPan(Vector2 mouseScreen)
        {
            Interaction.IsPanning = true;
            Interaction.LastMouseScreen = mouseScreen;
        }

        private void EndPan()
        {
            Interaction.IsPanning = false;
        }

        private void BeginPointDrag()
        {
            Interaction.IsDraggingPoints = true;
            Interaction.DragStartCanvas = Interaction.MouseDownCanvas;
            Interaction.DragPointStartPositions.Clear();

            foreach (var selected in Selection.Elements)
            {
                if (selected.Type != CanvasElementType.Point)
                    continue;

                if (TryGetPointById(selected.Id, out var point))
                    Interaction.DragPointStartPositions[selected.Id] = point.Position;
            }
        }

        private void DragSelectedPoints(Vector2 currentMouseScreen)
        {
            Vector2 currentCanvas = ScreenToCanvas(currentMouseScreen);
            Vector2 delta = currentCanvas - Interaction.DragStartCanvas;

            foreach (var kvp in Interaction.DragPointStartPositions)
            {
                int pointId = kvp.Key;
                Vector2 startPos = kvp.Value;
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

            foreach (var point in Document.Points)
            {
                Vector2 pointScreen = CanvasToScreen(point.Position);
                if (!marquee.Contains(pointScreen))
                    continue;

                var pointRef = CanvasElementRef.ForPoint(point.Id);

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

            foreach (var point in Document.Points)
                Selection.Add(CanvasElementRef.ForPoint(point.Id));

            foreach (var edge in Document.Edges)
                Selection.Add(CanvasElementRef.ForEdge(edge.Id));

            foreach (var offset in Document.Offsets)
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

        private void DrawGrid(Rect canvasRect)
        {
            Handles.BeginGUI();

            Color oldColor = Handles.color;
            Handles.color = new Color(1f, 1f, 1f, 0.05f);

            float spacing = GridSpacing * View.Zoom;
            if (spacing < 8f)
                spacing *= 2f;

            Vector2 offset = new Vector2(
                Mathf.Repeat(View.Pan.x, spacing),
                Mathf.Repeat(View.Pan.y, spacing));

            for (float x = canvasRect.xMin + offset.x; x < canvasRect.xMax; x += spacing)
                Handles.DrawLine(new Vector3(x, canvasRect.yMin), new Vector3(x, canvasRect.yMax));

            for (float y = canvasRect.yMin + offset.y; y < canvasRect.yMax; y += spacing)
                Handles.DrawLine(new Vector3(canvasRect.xMin, y), new Vector3(canvasRect.xMax, y));

            Handles.color = oldColor;
            Handles.EndGUI();
        }

        private void DrawMarquee(Rect canvasRect)
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

        private bool TryGetPointById(int pointId, out CanvasPoint point)
        {
            foreach (var p in Document.Points)
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

                var p = Document.Points[i];
                p.Position = position;
                Document.Points[i] = p;
                return;
            }
        }

        private bool TryGetEdgeScreenPositions(CanvasEdge edge, out Vector2 a, out Vector2 b)
        {
            a = default;
            b = default;

            if (!TryGetPointById(edge.A, out var pointA))
                return false;

            if (!TryGetPointById(edge.B, out var pointB))
                return false;

            a = CanvasToScreen(pointA.Position);
            b = CanvasToScreen(pointB.Position);
            return true;
        }

        private static Rect GetCanvasBounds(IList<CanvasPoint> points)
        {
            if (points == null || points.Count == 0)
                return new Rect(0f, 0f, 100f, 100f);

            Vector2 min = points[0].Position;
            Vector2 max = points[0].Position;

            for (int i = 1; i < points.Count; i++)
            {
                min = Vector2.Min(min, points[i].Position);
                max = Vector2.Max(max, points[i].Position);
            }

            Vector2 size = max - min;
            if (size.x < 0.001f) size.x = 1f;
            if (size.y < 0.001f) size.y = 1f;

            return new Rect(min, size);
        }
    }
}
#endif