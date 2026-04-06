using UnityEngine;

namespace DLN
{
    public class Edge : MonoBehaviour
    {
        public BezierCurve bezierCurve;
        public LineRenderer lineRenderer;
        // these are to already be assined to the bezier in the prefab
        // they are to have their own follow logic.
        [Header("Assigned at runtime")]
        public PosTargetProvider startTarget;
        public PosTargetProvider endTarget;
        [Header("Assigned to LineRenderer in the prefab")]
        [SerializeField] Transform startControlPoint;
        [SerializeField] Transform endControlPoint;

        public void Start()
        {
            UpdateVisibility();
        }
        // edge should not require a start point and end point in order to exist.
        public void SetStartPoint(IPortBase start)
        {
            startTarget.SetTarget(start.transform);
        }
        public void SetEndPoint(IPortBase end)
        {
            endTarget.SetTarget(end.transform);
        }
        public void SetLineThickness(float thickness)
        {
            if (lineRenderer != null)
            {
                lineRenderer.startWidth = thickness;
                lineRenderer.endWidth = thickness;
            }
        }
        public void SetStartWidth(float width)
        {
            if (lineRenderer != null)
            {
                lineRenderer.startWidth = width;
            }
        }
        public void SetEndWidth(float width)
        {
            if (lineRenderer != null)
            {
                lineRenderer.endWidth = width;
            }
        }
        public void UpdateVisibility()
        {
            if (startTarget.GetTarget() == null || endTarget.GetTarget() == null)
            {
                gameObject.SetActive(false);

            }
            else if (startTarget.GetTarget().gameObject.activeInHierarchy && endTarget.GetTarget().gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}