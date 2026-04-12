from pathlib import Path
import re
import sys

ROOT = Path.cwd()

CANVAS_POINT = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Model/CanvasPoint.cs"
PROFILE_POINT = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Model/ProfilePoint.cs"
PROFILE_X_SPAN = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Model/ProfileXSpan.cs"
PROFILE_Z_SPAN = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Model/ProfileZSpan.cs"
PROFILE_SPAN_LAYOUT = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Core/ProfileSpanLayout.cs"

PROFILE_DOC = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Documents/ProfileCanvasDocument.cs"
PROFILE_RESOLVER = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Core/ProfileCanvasPointResolver.cs"
PROFILE_POLICY = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Policy/ProfileCanvasPolicy.cs"
GENERATOR = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/ShapeStamperProfileGenerator.cs"
WINDOW = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/ShapeStamperWindow.cs"

OLD_PROFILE_ANCHOR_X = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Model/ProfileAnchorX.cs"
OLD_PROFILE_DEPTH_ANCHOR = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Model/ProfileDepthAnchor.cs"


def read(path: Path) -> str:
    if not path.exists():
        raise FileNotFoundError(f"Missing file: {path}")
    return path.read_text(encoding="utf-8")


def write(path: Path, text: str) -> None:
    path.write_text(text, encoding="utf-8", newline="\n")


def replace_or_fail(text: str, old: str, new: str, where: str) -> str:
    if old not in text:
        raise RuntimeError(f"Expected block not found in {where}:\\n{old[:220]}...")
    return text.replace(old, new, 1)


def ensure_model_files():
    write(CANVAS_POINT, """using System;
using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    [Serializable]
    public struct CanvasPoint
    {
        public int Id;
        public Vector2 Position;

        public CanvasAnchorX XAnchor;
        public CanvasAnchorY YAnchor;

        public float OffsetX;
        public float OffsetY;

        public CanvasPoint(int id, Vector2 position)
        {
            Id = id;
            Position = position;

            XAnchor = CanvasAnchorX.Floating;
            YAnchor = CanvasAnchorY.Floating;

            OffsetX = 0f;
            OffsetY = 0f;
        }
    }
}
""")

    write(PROFILE_X_SPAN, """namespace DLN.EditorTools.ShapeStamper
{
    public enum ProfileXSpan
    {
        PaddingToContent,
        ContentToBorder
    }
}
""")

    write(PROFILE_Z_SPAN, """namespace DLN.EditorTools.ShapeStamper
{
    public enum ProfileZSpan
    {
        PositiveBorderToContent,
        PositiveContentToPadding,
        MainDepth,
        NegativePaddingToContent,
        NegativeContentToBorder
    }
}
""")

    write(PROFILE_POINT, """using System;
using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    [Serializable]
    public struct ProfilePoint
    {
        public int Id;
        public Vector2 Position;

        public CanvasAnchorY YAnchor;
        public float OffsetY;

        public ProfileXSpan XSpan;
        public ProfileZSpan ZSpan;
        public float XT;
        public float ZT;

        public ProfilePoint(int id, Vector2 position)
        {
            Id = id;
            Position = position;

            YAnchor = CanvasAnchorY.Floating;
            OffsetY = 0f;

            XSpan = ProfileXSpan.PaddingToContent;
            ZSpan = ProfileZSpan.MainDepth;
            XT = 0f;
            ZT = 0f;
        }
    }
}
""")

    write(PROFILE_SPAN_LAYOUT, """using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    public readonly struct ProfileSpan
    {
        public readonly float Min;
        public readonly float Max;

        public ProfileSpan(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public float Evaluate(float t)
        {
            return Mathf.Lerp(Min, Max, Mathf.Clamp01(t));
        }

        public float InverseLerp(float value)
        {
            if (Mathf.Abs(Max - Min) < 0.000001f)
                return 0f;

            return Mathf.Clamp01(Mathf.InverseLerp(Min, Max, value));
        }
    }

    public readonly struct ProfileSpanLayoutData
    {
        public readonly ProfileSpan XPaddingToContent;
        public readonly ProfileSpan XContentToBorder;

        public readonly ProfileSpan ZPositiveBorderToContent;
        public readonly ProfileSpan ZPositiveContentToPadding;
        public readonly ProfileSpan ZMainDepth;
        public readonly ProfileSpan ZNegativePaddingToContent;
        public readonly ProfileSpan ZNegativeContentToBorder;

        public ProfileSpanLayoutData(
            ProfileSpan xPaddingToContent,
            ProfileSpan xContentToBorder,
            ProfileSpan zPositiveBorderToContent,
            ProfileSpan zPositiveContentToPadding,
            ProfileSpan zMainDepth,
            ProfileSpan zNegativePaddingToContent,
            ProfileSpan zNegativeContentToBorder)
        {
            XPaddingToContent = xPaddingToContent;
            XContentToBorder = xContentToBorder;
            ZPositiveBorderToContent = zPositiveBorderToContent;
            ZPositiveContentToPadding = zPositiveContentToPadding;
            ZMainDepth = zMainDepth;
            ZNegativePaddingToContent = zNegativePaddingToContent;
            ZNegativeContentToBorder = zNegativeContentToBorder;
        }

        public ProfileSpan GetXSpan(ProfileXSpan span)
        {
            switch (span)
            {
                case ProfileXSpan.PaddingToContent:
                    return XPaddingToContent;
                case ProfileXSpan.ContentToBorder:
                default:
                    return XContentToBorder;
            }
        }

        public ProfileSpan GetZSpan(ProfileZSpan span)
        {
            switch (span)
            {
                case ProfileZSpan.PositiveBorderToContent:
                    return ZPositiveBorderToContent;
                case ProfileZSpan.PositiveContentToPadding:
                    return ZPositiveContentToPadding;
                case ProfileZSpan.MainDepth:
                    return ZMainDepth;
                case ProfileZSpan.NegativePaddingToContent:
                    return ZNegativePaddingToContent;
                case ProfileZSpan.NegativeContentToBorder:
                default:
                    return ZNegativeContentToBorder;
            }
        }
    }

    public static class ProfileSpanLayout
    {
        public static ProfileSpanLayoutData Build(ProfileCanvasDocument document)
        {
            float xPadding = 0f;
            float xContent = Mathf.Max(xPadding, document.PaddingGuideX);
            float xBorder = Mathf.Max(xContent, document.BorderGuideX);

            float zTop = 0f;
            float zPosBorderToContentEnd = Mathf.Max(zTop, document.TopBorder);
            float zPosContentToPaddingEnd = Mathf.Max(zPosBorderToContentEnd, document.TopBorder + document.TopPadding);

            float zMainStart = zPosContentToPaddingEnd;
            float zMainEnd = Mathf.Max(zMainStart, document.WorldSizeMeters.y - document.BottomBorder - document.BottomPadding);

            float zNegPaddingToContentEnd = Mathf.Max(zMainEnd, document.WorldSizeMeters.y - document.BottomBorder);
            float zNegContentToBorderEnd = Mathf.Max(zNegPaddingToContentEnd, document.WorldSizeMeters.y);

            return new ProfileSpanLayoutData(
                xPaddingToContent: new ProfileSpan(xPadding, xContent),
                xContentToBorder: new ProfileSpan(xContent, xBorder),
                zPositiveBorderToContent: new ProfileSpan(zTop, zPosBorderToContentEnd),
                zPositiveContentToPadding: new ProfileSpan(zPosBorderToContentEnd, zPosContentToPaddingEnd),
                zMainDepth: new ProfileSpan(zMainStart, zMainEnd),
                zNegativePaddingToContent: new ProfileSpan(zMainEnd, zNegPaddingToContentEnd),
                zNegativeContentToBorder: new ProfileSpan(zNegPaddingToContentEnd, zNegContentToBorderEnd));
        }
    }
}
""")
    print("Wrote/normalized CanvasPoint and profile span model files")


def rewrite_profile_document():
    write(PROFILE_DOC, """using System;
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
""")
    print("Rewrote ProfileCanvasDocument.cs")


def rewrite_profile_resolver():
    write(PROFILE_RESOLVER, """using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    public static class ProfileCanvasPointResolver
    {
        public static Vector2 ResolvePoint(
            ProfilePoint point,
            Rect oldBounds,
            Rect newBounds,
            float oldPaddingGuideX,
            float oldBorderGuideX,
            float newPaddingGuideX,
            float newBorderGuideX)
        {
            ProfileCanvasDocument newDoc = BuildDoc(newBounds, newPaddingGuideX, newBorderGuideX);
            ProfileSpanLayoutData newLayout = ProfileSpanLayout.Build(newDoc);

            float x = newLayout.GetXSpan(point.XSpan).Evaluate(point.XT);
            float y = ResolveY(point, newBounds, newLayout);

            return new Vector2(x, y);
        }

        public static void SetSpansFromPosition(
            ref ProfilePoint point,
            Rect bounds,
            float paddingGuideX,
            float borderGuideX)
        {
            ProfileCanvasDocument doc = BuildDoc(bounds, paddingGuideX, borderGuideX);
            ProfileSpanLayoutData layout = ProfileSpanLayout.Build(doc);

            point.XSpan = DetectXSpan(point.Position.x, layout);
            point.ZSpan = DetectZSpan(point.Position.y, layout);

            point.XT = layout.GetXSpan(point.XSpan).InverseLerp(point.Position.x);
            point.ZT = layout.GetZSpan(point.ZSpan).InverseLerp(point.Position.y);
        }

        private static float ResolveY(ProfilePoint point, Rect bounds, ProfileSpanLayoutData layout)
        {
            if (point.YAnchor != CanvasAnchorY.Floating)
            {
                switch (point.YAnchor)
                {
                    case CanvasAnchorY.Top:
                        return bounds.yMin + point.OffsetY;
                    case CanvasAnchorY.Bottom:
                        return bounds.yMax + point.OffsetY;
                    case CanvasAnchorY.Center:
                        return bounds.center.y + point.OffsetY;
                }
            }

            return layout.GetZSpan(point.ZSpan).Evaluate(point.ZT);
        }

        private static ProfileXSpan DetectXSpan(float x, ProfileSpanLayoutData layout)
        {
            if (x <= layout.XPaddingToContent.Max)
                return ProfileXSpan.PaddingToContent;

            return ProfileXSpan.ContentToBorder;
        }

        private static ProfileZSpan DetectZSpan(float z, ProfileSpanLayoutData layout)
        {
            if (z <= layout.ZPositiveBorderToContent.Max)
                return ProfileZSpan.PositiveBorderToContent;
            if (z <= layout.ZPositiveContentToPadding.Max)
                return ProfileZSpan.PositiveContentToPadding;
            if (z <= layout.ZMainDepth.Max)
                return ProfileZSpan.MainDepth;
            if (z <= layout.ZNegativePaddingToContent.Max)
                return ProfileZSpan.NegativePaddingToContent;

            return ProfileZSpan.NegativeContentToBorder;
        }

        private static ProfileCanvasDocument BuildDoc(Rect bounds, float paddingGuideX, float borderGuideX)
        {
            ProfileCanvasDocument doc = new ProfileCanvasDocument();
            doc.ResizeWorld(new Vector2(Mathf.Max(0.0001f, bounds.width), Mathf.Max(0.0001f, bounds.height)));

            float borderOnly = Mathf.Max(0f, borderGuideX - paddingGuideX);

            doc.SetGuideValues(
                leftPadding: paddingGuideX,
                rightPadding: paddingGuideX,
                topPadding: paddingGuideX,
                bottomPadding: paddingGuideX,
                leftBorder: borderOnly,
                rightBorder: borderOnly,
                topBorder: borderOnly,
                bottomBorder: borderOnly);

            doc.FrontPaddingDepth = paddingGuideX;
            doc.FrontBorderDepth = borderOnly;

            return doc;
        }
    }
}
""")
    print("Rewrote ProfileCanvasPointResolver.cs")


def rewrite_profile_policy():
    write(PROFILE_POLICY, """#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    public sealed class ProfileCanvasPolicy : ICanvasToolPolicy
    {
        private readonly ProfileCanvasDocument _document;

        public ProfileCanvasPolicy(ProfileCanvasDocument document)
        {
            _document = document;
        }

        public void DrawOverlay(EditorCanvas canvas, Rect canvasRect)
        {
            if (_document == null)
                return;

            CanvasGuideDrawing.DrawProfileGuides(canvas, canvasRect, _document);

            Rect labelRect = new Rect(canvasRect.x + 8f, canvasRect.y + 8f, 260f, 20f);
            GUI.Label(
                labelRect,
                $"Profile  {_document.WorldSizeMeters.x:0.###}m x {_document.WorldSizeMeters.y:0.###}m",
                EditorStyles.miniLabel
            );
        }

        public void OnMouseDown(EditorCanvas canvas, Event evt) { }
        public void OnDrag(EditorCanvas canvas, Event evt) { }
        public void OnClick(EditorCanvas canvas, Event evt) { }

        public void OnKeyDown(EditorCanvas canvas, Event evt)
        {
            if (_document == null)
                return;

            if (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace)
            {
                DeleteSelection(canvas);
                evt.Use();
            }
        }

        public void AddPointAtCanvasPosition(EditorCanvas canvas, Vector2 canvasPos)
        {
            if (_document == null)
                return;

            int newPointId = _document.GetNextPointId();

            Vector2 newPos;
            if (_document.Points.Count == 0)
            {
                newPos = ClampToProfileBounds(canvasPos);
            }
            else if (_document.Points.Count == 1)
            {
                CanvasPoint last = _document.Points[_document.Points.Count - 1];
                newPos = ClampToProfileBounds(last.Position + new Vector2(0.15f, 0f));
            }
            else
            {
                CanvasPoint prev = _document.Points[_document.Points.Count - 2];
                CanvasPoint last = _document.Points[_document.Points.Count - 1];

                Vector2 dir = last.Position - prev.Position;
                if (dir.sqrMagnitude < 0.000001f)
                    dir = Vector2.right;
                else
                    dir.Normalize();

                float defaultLength = Mathf.Max(_document.WorldSizeMeters.x, _document.WorldSizeMeters.y) * 0.15f;
                newPos = ClampToProfileBounds(last.Position + dir * defaultLength);
            }

            ProfilePoint pp = new ProfilePoint
            {
                Id = newPointId,
                Position = newPos,
                YAnchor = CanvasAnchorY.Floating,
                XSpan = ProfileXSpan.PaddingToContent,
                ZSpan = ProfileZSpan.MainDepth,
                XT = 0f,
                ZT = 0.5f
            };

            ProfileCanvasPointResolver.SetSpansFromPosition(
                ref pp,
                _document.GetCanvasFrameRect(),
                _document.PaddingGuideX,
                _document.BorderGuideX);

            _document.ProfilePoints.Add(pp);
            _document.RebuildOpenEdges();
            _document.SyncDisplayPointsFromProfilePoints();
            _document.MarkDirty();

            canvas.Selection.Clear();
            canvas.Selection.Add(CanvasElementRef.ForPoint(newPointId));
        }

        public void SplitEdgeAtScreenPosition(EditorCanvas canvas, CanvasElementRef edgeRef, Vector2 screenPos)
        {
            if (_document == null || !edgeRef.IsEdge)
                return;

            if (!TryGetEdgeById(_document, edgeRef.Id, out CanvasEdge edge))
                return;

            if (!CanvasMath.TryGetEdgeCanvasPositions(edge, _document, out Vector2 a, out Vector2 b))
                return;

            Vector2 mouseCanvas = canvas.ScreenToCanvas(screenPos);
            Vector2 splitPoint = ClampToProfileBounds(CanvasMath.ClosestPointOnSegment(mouseCanvas, a, b));

            int newPointId = _document.GetNextPointId();
            int edgeIndex = GetEdgeIndexById(_document, edge.Id);
            if (edgeIndex < 0)
                return;

            ProfilePoint pp = new ProfilePoint
            {
                Id = newPointId,
                Position = splitPoint,
                YAnchor = CanvasAnchorY.Floating,
                XSpan = ProfileXSpan.PaddingToContent,
                ZSpan = ProfileZSpan.MainDepth,
                XT = 0f,
                ZT = 0.5f
            };

            ProfileCanvasPointResolver.SetSpansFromPosition(
                ref pp,
                _document.GetCanvasFrameRect(),
                _document.PaddingGuideX,
                _document.BorderGuideX);

            int insertIndex = edgeIndex + 1;
            _document.ProfilePoints.Insert(insertIndex, pp);

            _document.RebuildOpenEdges();
            RemapOffsetsAfterSplit(edge.Id, edgeIndex);
            _document.SyncDisplayPointsFromProfilePoints();
            _document.MarkDirty();

            canvas.Selection.Clear();
            canvas.Selection.Add(CanvasElementRef.ForPoint(newPointId));
        }

        public void DeleteSelection(EditorCanvas canvas)
        {
            if (_document == null)
                return;

            List<int> pointIdsToDelete = new();
            HashSet<int> edgeIdsToDelete = new();
            HashSet<int> offsetIdsToDelete = new();

            foreach (CanvasElementRef element in canvas.Selection.Elements)
            {
                switch (element.Type)
                {
                    case CanvasElementType.Point: pointIdsToDelete.Add(element.Id); break;
                    case CanvasElementType.Edge: edgeIdsToDelete.Add(element.Id); break;
                    case CanvasElementType.Offset: offsetIdsToDelete.Add(element.Id); break;
                }
            }

            if (pointIdsToDelete.Count > 0)
                DeletePointsAndConnectedData(pointIdsToDelete);

            if (edgeIdsToDelete.Count > 0)
            {
                RemoveEdgesById(edgeIdsToDelete);
                RemoveOffsetsByEdgeIds(edgeIdsToDelete);
            }

            if (offsetIdsToDelete.Count > 0)
                RemoveOffsetsById(offsetIdsToDelete);

            _document.RebuildOpenEdges();
            _document.SyncDisplayPointsFromProfilePoints();
            _document.MarkDirty();
            canvas.Selection.Clear();
            canvas.Interaction.Clear();
        }

        public void ConstrainDraggedPoint(EditorCanvas canvas, int pointId, ref Vector2 position)
        {
            position = ClampToProfileBounds(position);
        }

        private Vector2 ClampToProfileBounds(Vector2 p)
        {
            return new Vector2(
                Mathf.Clamp(p.x, 0f, _document.WorldSizeMeters.x),
                Mathf.Clamp(p.y, 0f, _document.WorldSizeMeters.y)
            );
        }

        private static bool TryGetEdgeById(ICanvasDocument document, int edgeId, out CanvasEdge edge)
        {
            foreach (CanvasEdge e in document.Edges)
            {
                if (e.Id == edgeId)
                {
                    edge = e;
                    return true;
                }
            }

            edge = default;
            return false;
        }

        private static int GetEdgeIndexById(ICanvasDocument document, int edgeId)
        {
            for (int i = 0; i < document.Edges.Count; i++)
            {
                if (document.Edges[i].Id == edgeId)
                    return i;
            }

            return -1;
        }

        private void DeletePointsAndConnectedData(List<int> pointIds)
        {
            HashSet<int> pointSet = new(pointIds);
            HashSet<int> deletedEdgeIds = new();

            for (int i = _document.Edges.Count - 1; i >= 0; i--)
            {
                CanvasEdge edge = _document.Edges[i];
                if (pointSet.Contains(edge.A) || pointSet.Contains(edge.B))
                {
                    deletedEdgeIds.Add(edge.Id);
                    _document.Edges.RemoveAt(i);
                }
            }

            for (int i = _document.Offsets.Count - 1; i >= 0; i--)
            {
                if (deletedEdgeIds.Contains(_document.Offsets[i].EdgeId))
                    _document.Offsets.RemoveAt(i);
            }

            for (int i = _document.ProfilePoints.Count - 1; i >= 0; i--)
            {
                if (pointSet.Contains(_document.ProfilePoints[i].Id))
                    _document.ProfilePoints.RemoveAt(i);
            }
        }

        private void RemoveEdgesById(HashSet<int> edgeIds)
        {
            for (int i = _document.Edges.Count - 1; i >= 0; i--)
            {
                if (edgeIds.Contains(_document.Edges[i].Id))
                    _document.Edges.RemoveAt(i);
            }
        }

        private void RemoveOffsetsByEdgeIds(HashSet<int> edgeIds)
        {
            for (int i = _document.Offsets.Count - 1; i >= 0; i--)
            {
                if (edgeIds.Contains(_document.Offsets[i].EdgeId))
                    _document.Offsets.RemoveAt(i);
            }
        }

        private void RemoveOffsetsById(HashSet<int> offsetIds)
        {
            for (int i = _document.Offsets.Count - 1; i >= 0; i--)
            {
                if (offsetIds.Contains(_document.Offsets[i].Id))
                    _document.Offsets.RemoveAt(i);
            }
        }

        private void RemapOffsetsAfterSplit(int oldEdgeId, int replacementEdgeIndex)
        {
            if (replacementEdgeIndex < 0 || replacementEdgeIndex >= _document.Edges.Count)
                return;

            int replacementEdgeId = _document.Edges[replacementEdgeIndex].Id;

            for (int i = 0; i < _document.Offsets.Count; i++)
            {
                CanvasOffsetConstraint offset = _document.Offsets[i];
                if (offset.EdgeId != oldEdgeId)
                    continue;

                offset.EdgeId = replacementEdgeId;
                _document.Offsets[i] = offset;
            }
        }
    }
}
#endif
""")
    print("Rewrote ProfileCanvasPolicy.cs")


def patch_generator():
    text = read(GENERATOR)
    text = text.replace("profileDocument.Points == null || profileDocument.Points.Count < 2", "profileDocument.ProfilePoints == null || profileDocument.ProfilePoints.Count < 2")
    text = text.replace("for (int i = 0; i < profileDocument.Points.Count; i++)", "for (int i = 0; i < profileDocument.ProfilePoints.Count; i++)")
    text = text.replace("ProfilePoint p = profileDocument.Points[i];", "ProfilePoint p = profileDocument.ProfilePoints[i];")
    text = text.replace("profileDocument.Points,", "profileDocument.ProfilePoints,")
    write(GENERATOR, text)
    print("Patched ShapeStamperProfileGenerator.cs")


def patch_window():
    text = read(WINDOW)

    text = text.replace(
        "        private void DrawSelectedShapeElementInspector()\n\n\n        {\n",
        "        private void DrawSelectedShapeElementInspector()\n        {\n"
    )

    old_method_start = "        private void DrawSelectedProfilePointInspector()\n        {\n"
    start = text.find(old_method_start)
    if start == -1:
        raise RuntimeError("Could not find DrawSelectedProfilePointInspector() in ShapeStamperWindow.cs")

    next_method = text.find("\n        private static CanvasElementRef GetSingleSelection(", start)
    if next_method == -1:
        raise RuntimeError("Could not find end of DrawSelectedProfilePointInspector() in ShapeStamperWindow.cs")

    new_method = """        private void DrawSelectedProfilePointInspector()
        {
            if (profileSelection == null || profileSelection.Count != 1)
                return;

            CanvasElementRef selected = GetSingleSelection(profileSelection);
            if (!selected.IsPoint)
                return;

            IList<ProfilePoint> points = profileDocument.ProfilePoints;
            int index = FindProfilePointIndex(points, selected.Id);
            if (index < 0)
                return;

            ProfilePoint point = points[index];
            Rect bounds = profileDocument.GetCanvasFrameRect();
            float paddingGuideX = profileDocument.PaddingGuideX;
            float borderGuideX = profileDocument.BorderGuideX;

            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Profile Point {point.Id}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                $"X:{point.XSpan}   Z:{point.ZSpan}   XT:{point.XT:0.###}   ZT:{point.ZT:0.###}",
                EditorStyles.miniLabel);

            EditorGUI.BeginChangeCheck();

            ProfileXSpan newXSpan = (ProfileXSpan)EditorGUILayout.EnumPopup("Profile X Span", point.XSpan);
            ProfileZSpan newZSpan = (ProfileZSpan)EditorGUILayout.EnumPopup("Profile Z Span", point.ZSpan);
            CanvasAnchorY newYAnchor = (CanvasAnchorY)EditorGUILayout.EnumPopup("Profile Y Anchor", point.YAnchor);
            Vector2 newPosition = EditorGUILayout.Vector2Field("Position", point.Position);
            float newXT = EditorGUILayout.Slider("Profile X T", point.XT, 0f, 1f);
            float newZT = EditorGUILayout.Slider("Profile Z T", point.ZT, 0f, 1f);

            bool canEditOffsetY = point.YAnchor != CanvasAnchorY.Floating;
            EditorGUI.BeginDisabledGroup(!canEditOffsetY);
            float newOffsetY = EditorGUILayout.FloatField("Offset Y", point.OffsetY);
            EditorGUI.EndDisabledGroup();

            if (EditorGUI.EndChangeCheck())
            {
                point.XSpan = newXSpan;
                point.ZSpan = newZSpan;
                point.YAnchor = newYAnchor;
                point.Position = new Vector2(
                    Mathf.Clamp(newPosition.x, 0f, profileDocument.WorldSizeMeters.x),
                    Mathf.Clamp(newPosition.y, 0f, profileDocument.WorldSizeMeters.y));
                point.XT = Mathf.Clamp01(newXT);
                point.ZT = Mathf.Clamp01(newZT);

                if (point.YAnchor != CanvasAnchorY.Floating)
                    point.OffsetY = newOffsetY;

                ProfileCanvasPointResolver.SetSpansFromPosition(
                    ref point,
                    bounds,
                    paddingGuideX,
                    borderGuideX);

                profileDocument.ProfilePoints[index] = point;
                profileDocument.SyncDisplayPointsFromProfilePoints();
                profileDocument.MarkDirty();
                _forcePreviewRefresh = true;
                Repaint();
            }

            EditorGUILayout.EndVertical();
        }

        private static int FindProfilePointIndex(IList<ProfilePoint> points, int pointId)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].Id == pointId)
                    return i;
            }

            return -1;
        }
"""
    text = text[:start] + new_method + text[next_method:]

    write(WINDOW, text)
    print("Patched ShapeStamperWindow.cs")


def patch_editor_canvas():
    path = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Core/EditorCanvas.cs"
    text = read(path)

    old = """                if (Document is ShapeCanvasDocument)
                {
                    ShapeCanvasPointResolver.RecalculateOffsets(ref p, bounds);
                }
                else if (Document is ProfileCanvasDocument profileDocument)
                {
                    ProfileCanvasPointResolver.RecalculateOffsets(
                        ref p,
                        bounds,
                        profileDocument.PaddingGuideX,
                        profileDocument.BorderGuideX);
                }

                Document.Points[i] = p;
                return;
"""
    new = """                if (Document is ShapeCanvasDocument)
                {
                    ShapeCanvasPointResolver.RecalculateOffsets(ref p, bounds);
                    Document.Points[i] = p;
                    return;
                }
                else if (Document is ProfileCanvasDocument profileDocument)
                {
                    profileDocument.SetProfilePointDisplayPosition(pointId, position);
                    return;
                }

                Document.Points[i] = p;
                return;
"""
    text = replace_or_fail(text, old, new, path.name)
    write(path, text)
    print("Patched EditorCanvas.cs")


def remove_old_anchor_files():
    for path in (OLD_PROFILE_ANCHOR_X, OLD_PROFILE_DEPTH_ANCHOR):
        if path.exists():
            path.unlink()
    print("Removed old profile anchor files if present")


def main():
    try:
        ensure_model_files()
        rewrite_profile_document()
        rewrite_profile_resolver()
        rewrite_profile_policy()
        patch_generator()
        patch_window()
        patch_editor_canvas()
        remove_old_anchor_files()
    except Exception as exc:
        print(f"Patch failed: {exc}", file=sys.stderr)
        return 1

    print()
    print("Done.")
    print("Next:")
    print("1. Let Unity recompile.")
    print("2. Reset Profile.")
    print("3. Confirm profile points edit spans/T values in the inspector.")
    print("4. Confirm dragging profile points still works.")
    print("5. If errors remain, they should now be a much smaller cleanup cluster.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())