using UnityEngine;

namespace DLN
{
    [System.Serializable]
    public class CanvasViewState
    {
        public Vector2 Pan = Vector2.zero;
        public float Zoom = 1f;

        public void FrameRect(Rect rect, Vector2 viewportSize, float padding)
        {
            if (viewportSize.x <= 0f || viewportSize.y <= 0f)
                return;

            float width = Mathf.Max(1f, rect.width);
            float height = Mathf.Max(1f, rect.height);

            float zoomX = (viewportSize.x - padding * 2f) / width;
            float zoomY = (viewportSize.y - padding * 2f) / height;

            Zoom = Mathf.Clamp(Mathf.Min(zoomX, zoomY), 0.05f, 10f);

            Vector2 rectCenter = rect.center;
            Vector2 viewportCenter = viewportSize * 0.5f;

            Pan = viewportCenter - rectCenter * Zoom;
        }
    }
}