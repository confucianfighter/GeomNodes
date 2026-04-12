#if false
using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    [System.Serializable]
    public class CanvasViewState
    {
        [SerializeField] private float worldPaddingPixels = 24f;

        public float WorldPaddingPixels
        {
            get => Mathf.Max(0f, worldPaddingPixels);
            set => worldPaddingPixels = Mathf.Max(0f, value);
        }

        public void ResetView()
        {
            // Intentionally no-op for now.
            // This canvas always fits the document world rect into the viewport.
        }

        public void FrameRect(Rect rect, Vector2 viewportSize, float padding)
        {
            // Intentionally no-op for now.
            // Kept only so existing callers do not break.
            WorldPaddingPixels = padding;
        }
    }
}
#endif
