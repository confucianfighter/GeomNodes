using UnityEngine;

namespace DLN
{
    [ExecuteAlways]
    [RequireComponent(typeof(LineRenderer))]
    public class StraightLineBetweenPoints : MonoBehaviour
    {
        public Transform pointA;
        public Transform pointB;
        public bool useLocalSpace = false;

        private LineRenderer lr;

        private void OnEnable()
        {
            EnsureLineRenderer();
            Refresh();
        }

        private void OnValidate()
        {
            EnsureLineRenderer();
            Refresh();
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying)
            {
                // In edit mode, keep it live while moving objects around.
                Refresh();
                return;
            }

            Refresh();
        }

        private void EnsureLineRenderer()
        {
            if (lr == null)
                lr = GetComponent<LineRenderer>();

            if (lr != null)
            {
                lr.positionCount = 2;
                lr.useWorldSpace = !useLocalSpace;
            }
        }

        public void Refresh()
        {
            EnsureLineRenderer();

            if (lr == null || pointA == null || pointB == null)
                return;

            if (useLocalSpace)
            {
                lr.SetPosition(0, transform.InverseTransformPoint(pointA.position));
                lr.SetPosition(1, transform.InverseTransformPoint(pointB.position));
            }
            else
            {
                lr.SetPosition(0, pointA.position);
                lr.SetPosition(1, pointB.position);
            }
        }
    }
}