using System.Collections.Generic;

namespace DLN.EditorTools.ShapeStamper
{
    [System.Serializable]
    public class CanvasSelection
    {
        public readonly HashSet<int> SelectedPointIds = new();
        public readonly HashSet<int> SelectedSegmentIds = new();

        public bool HasSelection => SelectedPointIds.Count > 0 || SelectedSegmentIds.Count > 0;

        public bool IsPointSelected(int pointId) => SelectedPointIds.Contains(pointId);
        public bool IsSegmentSelected(int segmentId) => SelectedSegmentIds.Contains(segmentId);

        public void Clear()
        {
            SelectedPointIds.Clear();
            SelectedSegmentIds.Clear();
        }

        public void SelectOnlyPoint(int pointId)
        {
            Clear();
            if (pointId >= 0)
                SelectedPointIds.Add(pointId);
        }

        public void SelectOnlySegment(int segmentId)
        {
            Clear();
            if (segmentId >= 0)
                SelectedSegmentIds.Add(segmentId);
        }

        public void AddPoint(int pointId)
        {
            if (pointId >= 0)
                SelectedPointIds.Add(pointId);
        }

        public void AddSegment(int segmentId)
        {
            if (segmentId >= 0)
                SelectedSegmentIds.Add(segmentId);
        }

        public void RemovePoint(int pointId)
        {
            SelectedPointIds.Remove(pointId);
        }

        public void RemoveSegment(int segmentId)
        {
            SelectedSegmentIds.Remove(segmentId);
        }

        public void TogglePoint(int pointId)
        {
            if (pointId < 0) return;

            if (!SelectedPointIds.Add(pointId))
                SelectedPointIds.Remove(pointId);
        }

        public void ToggleSegment(int segmentId)
        {
            if (segmentId < 0) return;

            if (!SelectedSegmentIds.Add(segmentId))
                SelectedSegmentIds.Remove(segmentId);
        }

        public void SetPointSelected(int pointId, bool selected)
        {
            if (pointId < 0) return;

            if (selected) SelectedPointIds.Add(pointId);
            else SelectedPointIds.Remove(pointId);
        }

        public void SetSegmentSelected(int segmentId, bool selected)
        {
            if (segmentId < 0) return;

            if (selected) SelectedSegmentIds.Add(segmentId);
            else SelectedSegmentIds.Remove(segmentId);
        }
    }
}