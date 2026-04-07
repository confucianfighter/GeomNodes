using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    [System.Serializable]
    public class CanvasViewState
    {
        public Vector2 Pan = Vector2.zero;
        public float Zoom = 1f;

        public const float MinZoom = 0.05f;
        public const float MaxZoom = 50f;

        public void ClampZoom()
        {
            Zoom = Mathf.Clamp(Zoom, MinZoom, MaxZoom);
        }

        public Vector2 CanvasToScreen(Vector2 canvasPosition, Rect canvasRect)
        {
            var center = canvasRect.center;
            return center + Pan + (canvasPosition * Zoom);
        }

        public Vector2 ScreenToCanvas(Vector2 screenPosition, Rect canvasRect)
        {
            var center = canvasRect.center;
            return (screenPosition - center - Pan) / Mathf.Max(Zoom, 0.0001f);
        }

        public float CanvasToScreenDistance(float canvasDistance)
        {
            return canvasDistance * Zoom;
        }

        public float ScreenToCanvasDistance(float screenDistance)
        {
            return screenDistance / Mathf.Max(Zoom, 0.0001f);
        }

        /// <summary>
        /// Zooms around a specific screen-space pivot so the canvas point under the mouse stays stable.
        /// </summary>
        public void ZoomAroundScreenPoint(float zoomDelta, Vector2 screenPivot, Rect canvasRect)
        {
            var before = ScreenToCanvas(screenPivot, canvasRect);

            Zoom *= zoomDelta;
            ClampZoom();

            var after = CanvasToScreen(before, canvasRect);
            Pan += screenPivot - after;
        }

        public void Reset()
        {
            Pan = Vector2.zero;
            Zoom = 1f;
        }
    }
}