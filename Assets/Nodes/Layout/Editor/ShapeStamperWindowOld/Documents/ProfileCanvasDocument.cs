#if false
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    [Serializable]
    public class ProfileCanvasDocument : ICanvasDocument, ICanvasBoundsProvider
    {
        [SerializeField] private Vector2 worldSizeMeters = new Vector2(1f, 1f);

        // Display proxy points for the shared canvas/editor stack.
        [SerializeField] private List<CanvasPoint> points = new();

        // Real authored profile model.
        [SerializeField] private List<ProfilePoint> profilePoints = new();

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
        public IList<ProfilePoint> ProfilePoints => profilePoints;
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

            if (profilePoints.Count == 0)
                ResetDefaultProfile();

            ClampAllProfilePointsToWorld();

            if (edges.Count == 0 && profilePoints.Count >= 2)
                RebuildOpenEdges();

            SyncDisplayPointsFromProfilePoints();
        }

        public void ResetDefaultProfile()
        {
            points.Clear();
            profilePoints.Clear();
            edges.Clear();
            offsets.Clear();

            profilePoints.Add(new ProfilePoint
            {
                Id = 0,
                Position = new Vector2(0.00f, 0.10f),
                YAnchor = CanvasAnchorY.Top,
                XSpan = ProfileXSpan.PaddingToContent,
                ZSpan = ProfileZSpan.PositiveContentToPadding,
                XT = 0f,
                ZT = 1f
            });

            profilePoints.Add(new ProfilePoint
            {
                Id = 1,
                Position = new Vector2(0.08f, 0.25f),
                YAnchor = CanvasAnchorY.Floating,
                XSpan = ProfileXSpan.PaddingToContent,
                ZSpan = ProfileZSpan.PositiveBorderToContent,
                XT = 1f,
                ZT = 1f
            });

            profilePoints.Add(new ProfilePoint
            {
                Id = 2,
                Position = new Vector2(0.16f, 0.55f),
                YAnchor = CanvasAnchorY.Bottom,
                XSpan = ProfileXSpan.ContentToBorder,
                ZSpan = ProfileZSpan.PositiveBorderToContent,
                XT = 1f,
                ZT = 0f
            });

            RebuildOpenEdges();
            SyncDisplayPointsFromProfilePoints();
            MarkDirty();
        }

        public Rect GetCanvasFrameRect()
        {
            return new Rect(0f, 0f, WorldSizeMeters.x, WorldSizeMeters.y);
        }

        public void ResizeWorld(Vector2 newSize)
        {
            WorldSizeMeters = new Vector2(
                Mathf.Max(0.0001f, newSize.x),
                Mathf.Max(0.0001f, newSize.y)
            );

            ClampAllProfilePointsToWorld();
            SyncDisplayPointsFromProfilePoints();
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
            LeftPadding = newLeftPadding;
            RightPadding = newRightPadding;
            TopPadding = newTopPadding;
            BottomPadding = newBottomPadding;

            LeftBorder = newLeftBorder;
            RightBorder = newRightBorder;
            TopBorder = newTopBorder;
            BottomBorder = newBottomBorder;

            SyncDisplayPointsFromProfilePoints();
            MarkDirty();
        }

        public void SyncDisplayPointsFromProfilePoints()
        {
            points.Clear();

            Rect bounds = GetCanvasFrameRect();
            float paddingGuideX = PaddingGuideX;
            float borderGuideX = BorderGuideX;

            for (int i = 0; i < profilePoints.Count; i++)
            {
                ProfilePoint pp = profilePoints[i];
                Vector2 resolved = ProfileCanvasPointResolver.ResolvePoint(
                    pp,
                    bounds,
                    bounds,
                    paddingGuideX,
                    borderGuideX,
                    paddingGuideX,
                    borderGuideX);

                CanvasPoint display = new CanvasPoint
                {
                    Id = pp.Id,
                    Position = resolved,
                    XAnchor = CanvasAnchorX.Floating,
                    YAnchor = pp.YAnchor,
                    OffsetX = 0f,
                    OffsetY = pp.OffsetY
                };

                points.Add(display);
            }
        }

        public void SetProfilePointDisplayPosition(int pointId, Vector2 displayPosition)
        {
            Rect bounds = GetCanvasFrameRect();

            for (int i = 0; i < profilePoints.Count; i++)
            {
                if (profilePoints[i].Id != pointId)
                    continue;

                ProfilePoint pp = profilePoints[i];
                pp.Position = new Vector2(
                    Mathf.Clamp(displayPosition.x, 0f, WorldSizeMeters.x),
                    Mathf.Clamp(displayPosition.y, 0f, WorldSizeMeters.y));

                ProfileCanvasPointResolver.SetSpansFromPosition(
                    ref pp,
                    bounds,
                    PaddingGuideX,
                    BorderGuideX);

                profilePoints[i] = pp;
                SyncDisplayPointsFromProfilePoints();
                return;
            }
        }

        public int GetNextPointId()
        {
            int maxId = -1;
            for (int i = 0; i < profilePoints.Count; i++)
                maxId = Mathf.Max(maxId, profilePoints[i].Id);
            return maxId + 1;
        }

        public int GetNextEdgeId()
        {
            int maxId = -1;
            for (int i = 0; i < edges.Count; i++)
                maxId = Mathf.Max(maxId, edges[i].Id);
            return maxId + 1;
        }

        public void RebuildOpenEdges()
        {
            edges.Clear();

            for (int i = 0; i < profilePoints.Count - 1; i++)
            {
                edges.Add(new CanvasEdge
                {
                    Id = i,
                    A = profilePoints[i].Id,
                    B = profilePoints[i + 1].Id,
                    ProfileXScale = 1f
                });
            }
        }

        private void ClampAllProfilePointsToWorld()
        {
            for (int i = 0; i < profilePoints.Count; i++)
            {
                ProfilePoint p = profilePoints[i];
                p.Position = new Vector2(
                    Mathf.Clamp(p.Position.x, 0f, WorldSizeMeters.x),
                    Mathf.Clamp(p.Position.y, 0f, WorldSizeMeters.y)
                );
                profilePoints[i] = p;
            }
        }
    }
}
#endif
