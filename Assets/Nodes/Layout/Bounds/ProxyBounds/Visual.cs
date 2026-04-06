using UnityEngine;

namespace DLN
{
    public class Visual : MonoBehaviour
    {
        public GameObject visual;

        [ContextMenu("Enable Visual")]
        public void EnableVisual()
        {
            if (visual != null)
                visual.SetActive(true);
        }

        [ContextMenu("Disable Visual")]
        public void DisableVisual()
        {
            if (visual != null)
                visual.SetActive(false);
        }

        public void SetVisualEnabled(bool enabled)
        {
            if (visual != null)
                visual.SetActive(enabled);
        }
    }
}