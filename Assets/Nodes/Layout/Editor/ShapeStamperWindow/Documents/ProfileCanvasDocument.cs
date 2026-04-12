using System;
using System.Collections.Generic;
using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    [Serializable]
    public class ProfileCanvasDocument : ICanvasDocument, ICanvasBoundsProvider
    {
        [SerializeField] private Vector2 worldSizeMeters = new Vector2(1f, 1f);
        [SerializeField] private List<CanvasPoint> points = new();
        [SerializeField] private List<CanvasEdge> edges = new();
        [SerializeField] private List<CanvasOffsetConstraint> offsets = new();

        [SerializeField] private float leftPadding;
        [SerializeField] private float rightPadding;
        [SerializeField] private float topPadding;
        [SerializeField] private float bottomPadding;

        [SerializeField] private float leftBorder;
        [SerializeField] private float rightBorder;
        [SerializeField] private float topBorder;
        [SerializeField] private float bottomBorder;

        [SerializeField] private float frontPaddingDepth;
        [SerializeField] private float frontBorderDepth;

        [SerializeField, HideInInspector] private int revision;

        public Vector2 WorldSizeMeters
        {
            get => worldSizeMeters;
            set => worldSizeMeters = new Vector2(
                Mathf.Max(0.0001f, value.x),
                Mathf.Max(0.0001f, value.y)
            );
        }

        public float LeftPadding { get => leftPadding; set => leftPadding = Mathf.Max(0f, value); }
        public float RightPadding { get => rightPadding; set => rightPadding = Mathf.Max(0f, value); }
        public float TopPadding { get => topPadding; set => topPadding = Mathf.Max(0f, value); }
        public float BottomPadding { get => bottomPadding; set => bottomPadding = Mathf.Max(0f, value); }

        public float LeftBorder { get => leftBorder; set => leftBorder = Mathf.Max(0f, value); }
        public float RightBorder { get => rightBorder; set => rightBorder = Mathf.Max(0f, value); }
        public float TopBorder { get => topBorder; set => topBorder = Mathf.Max(0f, value); }
        public float BottomBorder { get => bottomBorder; set => bottomBorder = Mathf.Max(0f, value); }

        public float FrontPaddingDepth { get => frontPaddingDepth; set => frontPaddingDepth = Mathf.Max(0f, value); }
        public float FrontBorderDepth { get => frontBorderDepth; set => frontBorderDepth = Mathf.Max(0f, value); }

        public float AveragePadding => (leftPadding + rightPadding + topPadding + bottomPadding) * 0.25f;
        public float AverageBorder => (leftBorder + rightBorder + topBorder + bottomBorder) * 0.25f;
        public float PaddingGuideX => Mathf.Clamp(AveragePadding, 0f, WorldSizeMeters.x);
        public float BorderGuideX => Mathf.Clamp(AveragePadding + AverageBorder, 0f, WorldSizeMeters.x);

        public IList<CanvasPoint> Points => points;
        public IList<CanvasEdge> Edges => edges;
        public IList<CanvasOffsetConstraint> Offsets => offsets;

        public bool IsClosed => false;
        public int Revision => revision;

        public void MarkDirty()
        {
            revision++;
        }

        public void EnsureValidProfile()
        {
            WorldSizeMeters = worldSizeMeters;

            if (points.Count == 0)
                ResetDefaultProfile();

            ClampAllPointsToWorld();

            if (edges.Count == 0 && points.Count >= 2)
                RebuildOpenEdges();
        }

        public void ResetDefaultProfile()
        {
            points.Clear();
            edges.Clear();
            offsets.Clear();

            points.Add(new CanvasPoint
            {
                Id = 0,
                Position = new Vector2(0.00f, 0.10f),
                ProfileXAnchor = ProfileAnchorX.Padding,
                ProfileZAnchor = ProfileDepthAnchor.Padding,
                YAnchor = CanvasAnchorY.Top
            });
            points.Add(new CanvasPoint
            {
                Id = 1,
                Position = new Vector2(0.08f, 0.25f),
                ProfileXAnchor = ProfileAnchorX.Content,
                ProfileZAnchor = ProfileDepthAnchor.Content,
                YAnchor = CanvasAnchorY.Floating
            });
            points.Add(new CanvasPoint
            {
                Id = 2,
                Position = new Vector2(0.16f, 0.55f),
                ProfileXAnchor = ProfileAnchorX.Border,
                ProfileZAnchor = ProfileDepthAnchor.Border,
                YAnchor = CanvasAnchorY.Bottom
            });

            RebuildOpenEdges();
            RefreshAnchoredPointsForGuideChange(0f, 0f);
            MarkDirty();
        }

        public Rect GetCanvasFrameRect()
        {
            return new Rect(0f, 0f, WorldSizeMeters.x, WorldSizeMeters.y);
        }

        public void ResizeWorld(Vector2 newSize)
        {
            Rect oldBounds = GetCanvasFrameRect();
            float oldPaddingGuideX = PaddingGuideX;
            float oldBorderGuideX = BorderGuideX;

            WorldSizeMeters = new Vector2(
                Mathf.Max(0.0001f, newSize.x),
                Mathf.Max(0.0001f, newSize.y)
            );

            Rect newBounds = GetCanvasFrameRect();
            float newPaddingGuideX = PaddingGuideX;
            float newBorderGuideX = BorderGuideX;

            ResizePointList(points, oldBounds, newBounds, oldPaddingGuideX, oldBorderGuideX, newPaddingGuideX, newBorderGuideX);
            MarkDirty();
        }

        public void SetGuideValues(
            float newLeftPadding,
            float newRightPadding,
            float newTopPadding,
            float newBottomPadding,
            float newLeftBorder,
            float newRightBorder,
            float newTopBorder,
            float newBottomBorder)
        {
            float oldPaddingGuideX = PaddingGuideX;
            float oldBorderGuideX = BorderGuideX;

            LeftPadding = newLeftPadding;
            RightPadding = newRightPadding;
            TopPadding = newTopPadding;
            BottomPadding = newBottomPadding;

            LeftBorder = newLeftBorder;
            RightBorder = newRightBorder;
            TopBorder = newTopBorder;
            BottomBorder = newBottomBorder;

            RefreshAnchoredPointsForGuideChange(oldPaddingGuideX, oldBorderGuideX);
            MarkDirty();
        }

        public void RefreshAnchoredPointsForGuideChange(float oldPaddingGuideX, float oldBorderGuideX)
        {
            Rect bounds = GetCanvasFrameRect();
            float newPaddingGuideX = PaddingGuideX;
            float newBorderGuideX = BorderGuideX;

            for (int i = 0; i < points.Count; i++)
            {
                CanvasPoint p = points[i];
                ProfileCanvasPointResolver.ResizePointPreservingBehavior(
                    ref p,
                    bounds,
                    bounds,
                    oldPaddingGuideX,
                    oldBorderGuideX,
                    newPaddingGuideX,
                    newBorderGuideX);
                points[i] = p;
            }
        }

        private static void ResizePointList(
            List<CanvasPoint> list,
            Rect oldBounds,
            Rect newBounds,
            float oldPaddingGuideX,
            float oldBorderGuideX,
            float newPaddingGuideX,
            float newBorderGuideX)
        {
            if (list == null)
                return;

            for (int i = 0; i < list.Count; i++)
            {
                CanvasPoint p = list[i];
                ProfileCanvasPointResolver.ResizePointPreservingBehavior(
                    ref p,
                    oldBounds,
                    newBounds,
                    oldPaddingGuideX,
                    oldBorderGuideX,
                    newPaddingGuideX,
                    newBorderGuideX);
                list[i] = p;
            }
        }

        private void ClampAllPointsToWorld()
        {
            for (int i = 0; i < points.Count; i++)
            {
                CanvasPoint p = points[i];
                p.Position = new Vector2(
                    Mathf.Clamp(p.Position.x, 0f, WorldSizeMeters.x),
                    Mathf.Clamp(p.Position.y, 0f, WorldSizeMeters.y)
                );
                points[i] = p;
            }
        }

        private void RebuildOpenEdges()
        {
            edges.Clear();

            for (int i = 0; i < points.Count - 1; i++)
            {
                edges.Add(new CanvasEdge
                {
                    Id = i,
                    A = points[i].Id,
                    B = points[i + 1].Id,
                    ProfileXScale = 1f
                });
            }
        }
    }
}
