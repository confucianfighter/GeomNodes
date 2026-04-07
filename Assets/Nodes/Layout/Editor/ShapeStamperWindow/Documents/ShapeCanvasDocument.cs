using System.Collections.Generic;
using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    [System.Serializable]
    public class ShapeCanvasDocument
    {
        [SerializeField] private Vector2 worldSizeMeters = new Vector2(1f, 1f);
        [SerializeField] private List<Vector2> points = new();

        public Vector2 WorldSizeMeters
        {
            get => worldSizeMeters;
            set
            {
                Vector2 clamped = new Vector2(
                    Mathf.Max(0.0001f, value.x),
                    Mathf.Max(0.0001f, value.y)
                );

                if (Approximately(worldSizeMeters, clamped))
                    return;

                RescalePointsForWorldSizeChange(worldSizeMeters, clamped);
                worldSizeMeters = clamped;
                ClampAllPointsToWorldBounds();
            }
        }

        public List<Vector2> Points => points;

        public int PointCount => points?.Count ?? 0;

        public void EnsureValidShape()
        {
            if (points == null)
                points = new List<Vector2>();

            if (worldSizeMeters.x <= 0f || worldSizeMeters.y <= 0f)
                worldSizeMeters = new Vector2(1f, 1f);

            if (points.Count < 3)
                ResetToDefaultTriangle();

            ClampAllPointsToWorldBounds();
        }

        public void ResetToDefaultTriangle()
        {
            points = new List<Vector2>
            {
                new Vector2(worldSizeMeters.x * 0.5f, worldSizeMeters.y * 0.15f),
                new Vector2(worldSizeMeters.x * 0.15f, worldSizeMeters.y * 0.85f),
                new Vector2(worldSizeMeters.x * 0.85f, worldSizeMeters.y * 0.85f),
            };
        }

        public void ClampAllPointsToWorldBounds()
        {
            for (int i = 0; i < points.Count; i++)
                points[i] = ClampToWorldBounds(points[i]);
        }

        public Vector2 ClampToWorldBounds(Vector2 point)
        {
            return new Vector2(
                Mathf.Clamp(point.x, 0f, worldSizeMeters.x),
                Mathf.Clamp(point.y, 0f, worldSizeMeters.y)
            );
        }

        public void InsertMidpointOnEdge(int edgeIndex)
        {
            if (points == null || points.Count < 2)
                return;

            if (edgeIndex < 0 || edgeIndex >= points.Count)
                return;

            int next = (edgeIndex + 1) % points.Count;
            Vector2 a = points[edgeIndex];
            Vector2 b = points[next];
            Vector2 mid = (a + b) * 0.5f;
            points.Insert(edgeIndex + 1, mid);
        }

        public void MovePoint(int index, Vector2 delta)
        {
            if (index < 0 || index >= points.Count)
                return;

            points[index] = ClampToWorldBounds(points[index] + delta);
        }

        public void MovePoints(IEnumerable<int> indices, Vector2 delta)
        {
            if (indices == null)
                return;

            foreach (int index in indices)
            {
                if (index < 0 || index >= points.Count)
                    continue;

                points[index] = ClampToWorldBounds(points[index] + delta);
            }
        }

        public void DeletePoints(IEnumerable<int> indices)
        {
            if (indices == null || points == null || points.Count <= 3)
                return;

            int maxRemovable = points.Count - 3;
            if (maxRemovable <= 0)
                return;

            List<int> sorted = new List<int>();

            foreach (int index in indices)
            {
                if (index >= 0 && index < points.Count && !sorted.Contains(index))
                    sorted.Add(index);
            }

            if (sorted.Count == 0)
                return;

            sorted.Sort();
            sorted.Reverse();

            int removed = 0;
            for (int i = 0; i < sorted.Count; i++)
            {
                if (removed >= maxRemovable)
                    break;

                points.RemoveAt(sorted[i]);
                removed++;
            }
        }

        public void PruneInvalidPointIndices(ICollection<int> indices)
        {
            if (indices == null)
                return;

            List<int> invalid = null;

            foreach (int index in indices)
            {
                if (index < 0 || index >= points.Count)
                {
                    invalid ??= new List<int>();
                    invalid.Add(index);
                }
            }

            if (invalid == null)
                return;

            for (int i = 0; i < invalid.Count; i++)
                indices.Remove(invalid[i]);
        }

        public void PruneInvalidEdgeIndices(ICollection<int> indices)
        {
            if (indices == null)
                return;

            List<int> invalid = null;

            foreach (int index in indices)
            {
                if (index < 0 || index >= points.Count)
                {
                    invalid ??= new List<int>();
                    invalid.Add(index);
                }
            }

            if (invalid == null)
                return;

            for (int i = 0; i < invalid.Count; i++)
                indices.Remove(invalid[i]);
        }

        private void RescalePointsForWorldSizeChange(Vector2 oldWorldSize, Vector2 newWorldSize)
        {
            float scaleX = oldWorldSize.x > 0f ? newWorldSize.x / oldWorldSize.x : 1f;
            float scaleY = oldWorldSize.y > 0f ? newWorldSize.y / oldWorldSize.y : 1f;

            for (int i = 0; i < points.Count; i++)
            {
                Vector2 p = points[i];
                p.x *= scaleX;
                p.y *= scaleY;
                points[i] = p;
            }
        }

        private static bool Approximately(Vector2 a, Vector2 b)
        {
            return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y);
        }
    }
}