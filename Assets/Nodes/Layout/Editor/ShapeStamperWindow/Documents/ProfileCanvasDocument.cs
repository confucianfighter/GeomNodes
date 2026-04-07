using System.Collections.Generic;
using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    [System.Serializable]
    public class ProfileCanvasDocument
    {
        [SerializeField] private Vector2 worldSizeMeters = new Vector2(1f, 1f);
        [SerializeField] private List<Vector2> points = new();

        public Vector2 WorldSizeMeters => worldSizeMeters;
        public List<Vector2> Points => points;
        public int PointCount => points?.Count ?? 0;

        public void EnsureValidProfile()
        {
            if (points == null)
                points = new List<Vector2>();

            if (worldSizeMeters.x <= 0f || worldSizeMeters.y <= 0f)
                worldSizeMeters = new Vector2(1f, 1f);

            if (points.Count < 2)
                ResetToDefaultProfile();
        }

        public void ResetToDefaultProfile()
        {
            points = new List<Vector2>
            {
                new Vector2(0.15f, 0.8f),
                new Vector2(0.85f, 0.2f),
            };
        }
    }
}