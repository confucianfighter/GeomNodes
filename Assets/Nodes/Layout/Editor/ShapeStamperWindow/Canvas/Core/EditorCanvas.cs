using UnityEditor;
using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    [System.Serializable]
    public class EditorCanvas
    {
        public Rect ScreenRect;

        public readonly CanvasViewState View = new();
        public readonly CanvasInteractionState Interaction = new();
        public readonly CanvasSelection Selection = new();

        public bool IsMouseOver => ScreenRect.Contains(Event.current.mousePosition);

        public float PointHitRadiusPixels = 10f;
        public float SegmentHitDistancePixels = 6f;
        public float DragThresholdPixels = 4f;
        public float GridMinorSpacingCanvas = 0.25f;
        public float GridMajorSpacingCanvas = 1f;

        public Vector2 MouseCanvasPosition => View.ScreenToCanvas(Event.current.mousePosition, ScreenRect);

        public void SetScreenRect(Rect rect)
        {
            ScreenRect = rect;
        }

        public bool ContainsScreenPoint(Vector2 screenPoint)
        {
            return ScreenRect.Contains(screenPoint);
        }

        public Vector2 ScreenToCanvas(Vector2 screenPosition)
        {
            return View.ScreenToCanvas(screenPosition, ScreenRect);
        }

        public Vector2 CanvasToScreen(Vector2 canvasPosition)
        {
            return View.CanvasToScreen(canvasPosition, ScreenRect);
        }

        public float ScreenToCanvasDistance(float screenDistance)
        {
            return View.ScreenToCanvasDistance(screenDistance);
        }

        public float CanvasToScreenDistance(float canvasDistance)
        {
            return View.CanvasToScreenDistance(canvasDistance);
        }

        public void BeginFrame()
        {
            Interaction.UpdateMousePosition(Event.current.mousePosition);
        }

        public void HandleZoom(Event e, float zoomStep = 1.1f)
        {
            if (!ContainsScreenPoint(e.mousePosition))
                return;

            if (e.type != EventType.ScrollWheel)
                return;

            float zoomDelta = e.delta.y < 0f ? zoomStep : (1f / zoomStep);
            View.ZoomAroundScreenPoint(zoomDelta, e.mousePosition, ScreenRect);

            e.Use();
        }

        public void HandleMiddleMousePan(Event e)
        {
            if (!ContainsScreenPoint(e.mousePosition))
                return;

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 2)
                    {
                        Interaction.BeginMouseDown(e.mousePosition, ScreenToCanvas(e.mousePosition), e.button);
                        Interaction.BeginPan();
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (Interaction.IsPanning && Interaction.ActiveMouseButton == 2)
                    {
                        View.Pan += e.delta;
                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (Interaction.IsPanning && Interaction.ActiveMouseButton == 2)
                    {
                        Interaction.EndMouseInteraction();
                        e.Use();
                    }
                    break;
            }
        }

        public void HandleAltLeftMousePan(Event e)
        {
            if (!ContainsScreenPoint(e.mousePosition))
                return;

            bool wantsAltPan = e.alt && e.button == 0;

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (wantsAltPan)
                    {
                        Interaction.BeginMouseDown(e.mousePosition, ScreenToCanvas(e.mousePosition), e.button);
                        Interaction.BeginPan();
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (Interaction.IsPanning && Interaction.ActiveMouseButton == 0 && e.alt)
                    {
                        View.Pan += e.delta;
                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (Interaction.IsPanning && Interaction.ActiveMouseButton == 0)
                    {
                        Interaction.EndMouseInteraction();
                        e.Use();
                    }
                    break;
            }
        }

        public void BeginPrimaryMouseDown(Event e)
        {
            if (!ContainsScreenPoint(e.mousePosition))
                return;

            if (e.type != EventType.MouseDown || e.button != 0 || e.alt)
                return;

            Interaction.BeginMouseDown(e.mousePosition, ScreenToCanvas(e.mousePosition), e.button);
        }

        public void BeginMarqueeIfNeeded(Event e)
        {
            if (!Interaction.HasMouseDown || Interaction.ActiveMouseButton != 0)
                return;

            if (Interaction.IsDraggingSelection || Interaction.IsPanning || Interaction.IsMarqueeSelecting)
                return;

            if (!Interaction.HasMovedPastDragThreshold(DragThresholdPixels))
                return;

            if (Interaction.PressedPointId < 0 && Interaction.PressedSegmentId < 0)
            {
                Interaction.BeginMarquee(Interaction.MouseDownScreenPosition);
                Interaction.UpdateMarquee(e.mousePosition);
            }
        }

        public void UpdateMarquee(Event e)
        {
            if (Interaction.IsMarqueeSelecting)
            {
                Interaction.UpdateMarquee(e.mousePosition);
            }
        }

        public void EndPrimaryMouse(Event e)
        {
            if (e.button != 0)
                return;

            if (e.type == EventType.MouseUp)
            {
                Interaction.EndMouseInteraction();
            }
        }

        public void ClearHoverIfMouseLeaves()
        {
            if (!ContainsScreenPoint(Event.current.mousePosition))
                Interaction.ClearHover();
        }

        public bool ShouldClearSelectionOnEmptyClick(bool additive)
        {
            return !additive
                && Interaction.PressedPointId < 0
                && Interaction.PressedSegmentId < 0
                && !Interaction.IsMarqueeSelecting
                && !Interaction.IsDraggingSelection;
        }

        public bool IsAdditiveSelection(Event e)
        {
            return e.shift || e.control || e.command;
        }

        public Rect GetContentRect(float padding = 0f)
        {
            return new Rect(
                ScreenRect.x + padding,
                ScreenRect.y + padding,
                ScreenRect.width - padding * 2f,
                ScreenRect.height - padding * 2f
            );
        }

        public void FramePoints(System.Collections.Generic.IReadOnlyList<Vector2> points, float paddingPixels = 40f)
        {
            if (points == null || points.Count == 0)
                return;

            Vector2 min = points[0];
            Vector2 max = points[0];

            for (int i = 1; i < points.Count; i++)
            {
                min = Vector2.Min(min, points[i]);
                max = Vector2.Max(max, points[i]);
            }

            Vector2 size = max - min;
            Vector2 center = (min + max) * 0.5f;

            var usable = GetContentRect(paddingPixels);
            if (usable.width <= 1f || usable.height <= 1f)
                return;

            float zoomX = size.x > 0.0001f ? usable.width / size.x : 1f;
            float zoomY = size.y > 0.0001f ? usable.height / size.y : 1f;

            View.Zoom = Mathf.Clamp(Mathf.Min(zoomX, zoomY), CanvasViewState.MinZoom, CanvasViewState.MaxZoom);
            View.Pan = usable.center - ScreenRect.center - (center * View.Zoom);
        }

        public void DrawBackground(Color color)
        {
            EditorGUI.DrawRect(ScreenRect, color);
        }

        public void DrawBorder(Color color, float thickness = 1f)
        {
            EditorGUI.DrawRect(new Rect(ScreenRect.xMin, ScreenRect.yMin, ScreenRect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(ScreenRect.xMin, ScreenRect.yMax - thickness, ScreenRect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(ScreenRect.xMin, ScreenRect.yMin, thickness, ScreenRect.height), color);
            EditorGUI.DrawRect(new Rect(ScreenRect.xMax - thickness, ScreenRect.yMin, thickness, ScreenRect.height), color);
        }

        public void DrawMarquee(Color fillColor, Color borderColor, float borderThickness = 1f)
        {
            if (!Interaction.IsMarqueeSelecting)
                return;

            var r = Interaction.MarqueeRectScreen;
            EditorGUI.DrawRect(r, fillColor);

            EditorGUI.DrawRect(new Rect(r.xMin, r.yMin, r.width, borderThickness), borderColor);
            EditorGUI.DrawRect(new Rect(r.xMin, r.yMax - borderThickness, r.width, borderThickness), borderColor);
            EditorGUI.DrawRect(new Rect(r.xMin, r.yMin, borderThickness, r.height), borderColor);
            EditorGUI.DrawRect(new Rect(r.xMax - borderThickness, r.yMin, borderThickness, r.height), borderColor);
        }
    }
}