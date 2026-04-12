from pathlib import Path
import sys

ROOT = Path.cwd()

PROFILE_POINT = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Model/ProfilePoint.cs"
PROFILE_X_SPAN = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Model/ProfileXSpan.cs"
PROFILE_Z_SPAN = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Model/ProfileZSpan.cs"
PROFILE_SPAN_LAYOUT = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Core/ProfileSpanLayout.cs"

PROFILE_DOC = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Documents/ProfileCanvasDocument.cs"
PROFILE_RESOLVER = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Core/ProfileCanvasPointResolver.cs"
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


def write_new_model_files():
    write(PROFILE_X_SPAN, """namespace DLN
{
    public enum ProfileXSpan
    {
        PaddingToContent,
        ContentToBorder
    }
}
""")

    write(PROFILE_Z_SPAN, """namespace DLN
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

namespace DLN
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

namespace DLN
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
    print("Wrote new profile span model files")


def patch_profile_document():
    text = read(PROFILE_DOC)

    text = text.replace("private List<CanvasPoint> points = new();", "private List<ProfilePoint> points = new();")
    text = text.replace("public IList<CanvasPoint> Points => points;", "public IList<ProfilePoint> Points => points;")

    old_default = """            points.Add(new CanvasPoint
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
"""
    new_default = """            points.Add(new ProfilePoint
            {
                Id = 0,
                Position = new Vector2(0.00f, 0.10f),
                YAnchor = CanvasAnchorY.Top,
                XSpan = ProfileXSpan.PaddingToContent,
                ZSpan = ProfileZSpan.PositiveContentToPadding,
                XT = 0f,
                ZT = 1f
            });
            points.Add(new ProfilePoint
            {
                Id = 1,
                Position = new Vector2(0.08f, 0.25f),
                YAnchor = CanvasAnchorY.Floating,
                XSpan = ProfileXSpan.PaddingToContent,
                ZSpan = ProfileZSpan.PositiveBorderToContent,
                XT = 1f,
                ZT = 1f
            });
            points.Add(new ProfilePoint
            {
                Id = 2,
                Position = new Vector2(0.16f, 0.55f),
                YAnchor = CanvasAnchorY.Bottom,
                XSpan = ProfileXSpan.ContentToBorder,
                ZSpan = ProfileZSpan.PositiveBorderToContent,
                XT = 1f,
                ZT = 0f
            });
"""
    if old_default not in text:
        raise RuntimeError("Default profile point block in ProfileCanvasDocument.cs did not match current repo state.")
    text = text.replace(old_default, new_default, 1)

    text = text.replace("CanvasPoint p = points[i];", "ProfilePoint p = points[i];")
    text = text.replace("List<CanvasPoint> list,", "List<ProfilePoint> list,")

    write(PROFILE_DOC, text)
    print("Patched ProfileCanvasDocument.cs")


def rewrite_profile_resolver():
    write(PROFILE_RESOLVER, """using UnityEngine;

namespace DLN
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

        public static void ResizePointPreservingBehavior(
            ref ProfilePoint point,
            Rect oldBounds,
            Rect newBounds,
            float oldPaddingGuideX,
            float oldBorderGuideX,
            float newPaddingGuideX,
            float newBorderGuideX)
        {
            point.Position = ResolvePoint(
                point,
                oldBounds,
                newBounds,
                oldPaddingGuideX,
                oldBorderGuideX,
                newPaddingGuideX,
                newBorderGuideX);
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


def patch_generator():
    text = read(GENERATOR)

    text = text.replace("IList<CanvasPoint> profilePoints", "IList<ProfilePoint> profilePoints")
    text = text.replace("CanvasPoint p = profileDocument.Points[i];", "ProfilePoint p = profileDocument.Points[i];")
    text = text.replace("CanvasPoint Point;", "ProfilePoint Point;")
    text = text.replace("public CanvasPoint Point;", "public ProfilePoint Point;")
    text = text.replace("CanvasPoint point = profilePoints[pointIndex];", "ProfilePoint point = profilePoints[pointIndex];")

    old_x = """        private static float ResolveProfilePointX(int pointIndex, IList<ProfilePoint> profilePoints, EdgeProfileDrive drive)
        {
            if (profilePoints == null || pointIndex < 0 || pointIndex >= profilePoints.Count)
                return 0f;

            ProfilePoint point = profilePoints[pointIndex];
            if (point.ProfileXAnchor != ProfileAnchorX.Floating)
                return Mathf.Max(0f, ResolveDirectAnchorX(point, drive));

            int previousAnchorIndex = FindPreviousAnchorIndex(profilePoints, pointIndex);
            int nextAnchorIndex = FindNextAnchorIndex(profilePoints, pointIndex);

            if (previousAnchorIndex >= 0 && nextAnchorIndex >= 0)
            {
                float authoredA = profilePoints[previousAnchorIndex].Position.x;
                float authoredB = profilePoints[nextAnchorIndex].Position.x;
                float resolvedA = ResolveDirectAnchorX(profilePoints[previousAnchorIndex], drive);
                float resolvedB = ResolveDirectAnchorX(profilePoints[nextAnchorIndex], drive);

                float span = authoredB - authoredA;
                if (Mathf.Abs(span) <= SafeEpsilon)
                    return Mathf.Max(0f, resolvedA);

                float t = Mathf.Clamp01((point.Position.x - authoredA) / span);
                return Mathf.Max(0f, Mathf.Lerp(resolvedA, resolvedB, t));
            }

            if (previousAnchorIndex >= 0)
            {
                float authoredA = profilePoints[previousAnchorIndex].Position.x;
                float resolvedA = ResolveDirectAnchorX(profilePoints[previousAnchorIndex], drive);
                float authoredMax = GetAuthoredMaxX(profilePoints);
                float resolvedMax = GetResolvedOverallMaxX(profilePoints, drive);

                float span = authoredMax - authoredA;
                if (Mathf.Abs(span) <= SafeEpsilon)
                    return Mathf.Max(0f, resolvedA);

                float t = Mathf.Clamp01((point.Position.x - authoredA) / span);
                return Mathf.Max(0f, Mathf.Lerp(resolvedA, resolvedMax, t));
            }

            if (nextAnchorIndex >= 0)
            {
                float authoredB = profilePoints[nextAnchorIndex].Position.x;
                float resolvedB = ResolveDirectAnchorX(profilePoints[nextAnchorIndex], drive);
                float authoredMin = GetAuthoredMinX(profilePoints);
                float resolvedMin = GetResolvedOverallMinX(profilePoints, drive);

                float span = authoredB - authoredMin;
                if (Mathf.Abs(span) <= SafeEpsilon)
                    return Mathf.Max(0f, resolvedB);

                float t = Mathf.Clamp01((point.Position.x - authoredMin) / span);
                return Mathf.Max(0f, Mathf.Lerp(resolvedMin, resolvedB, t));
            }

            return Mathf.Max(0f, SafeFinite(point.Position.x * drive.Scale, 0f));
        }
"""
    new_x = """        private static float ResolveProfilePointX(int pointIndex, IList<ProfilePoint> profilePoints, EdgeProfileDrive drive)
        {
            if (profilePoints == null || pointIndex < 0 || pointIndex >= profilePoints.Count)
                return 0f;

            ProfilePoint point = profilePoints[pointIndex];

            float paddingWidth = Mathf.Max(0f, drive.BlendedPadding);
            float borderWidth = Mathf.Max(0f, drive.BlendedBorder);

            ProfileSpan paddingToContent = new ProfileSpan(0f, paddingWidth);
            ProfileSpan contentToBorder = new ProfileSpan(paddingWidth, paddingWidth + borderWidth);

            float raw;
            switch (point.XSpan)
            {
                case ProfileXSpan.PaddingToContent:
                    raw = paddingToContent.Evaluate(point.XT);
                    break;

                case ProfileXSpan.ContentToBorder:
                default:
                    raw = contentToBorder.Evaluate(point.XT);
                    break;
            }

            return Mathf.Max(0f, SafeFinite(raw * drive.Scale, 0f));
        }
"""
    if old_x not in text:
        raise RuntimeError("Could not find old ResolveProfilePointX block in ShapeStamperProfileGenerator.cs")
    text = text.replace(old_x, new_x, 1)

    old_z = """        private static float ResolveProfilePointZ(int pointIndex, IList<ProfilePoint> profilePoints, ProfileCanvasDocument profileDocument)
        {
            if (profilePoints == null || pointIndex < 0 || pointIndex >= profilePoints.Count)
                return 0f;

            ProfilePoint point = profilePoints[pointIndex];
            if (point.ProfileZAnchor != ProfileDepthAnchor.Floating)
                return Mathf.Max(0f, ResolveDirectAnchorZ(point, profileDocument));

            int previousAnchorIndex = FindPreviousDepthAnchorIndex(profilePoints, pointIndex);
            int nextAnchorIndex = FindNextDepthAnchorIndex(profilePoints, pointIndex);

            if (previousAnchorIndex >= 0 && nextAnchorIndex >= 0)
            {
                float authoredA = profilePoints[previousAnchorIndex].Position.y;
                float authoredB = profilePoints[nextAnchorIndex].Position.y;
                float resolvedA = ResolveDirectAnchorZ(profilePoints[previousAnchorIndex], profileDocument);
                float resolvedB = ResolveDirectAnchorZ(profilePoints[nextAnchorIndex], profileDocument);

                float span = authoredB - authoredA;
                if (Mathf.Abs(span) <= SafeEpsilon)
                    return Mathf.Max(0f, resolvedA);

                float t = Mathf.Clamp01((point.Position.y - authoredA) / span);
                return Mathf.Max(0f, Mathf.Lerp(resolvedA, resolvedB, t));
            }

            return Mathf.Max(0f, SafeFinite(point.Position.y, 0f));
        }
"""
    new_z = """        private static float ResolveProfilePointZ(int pointIndex, IList<ProfilePoint> profilePoints, ProfileCanvasDocument profileDocument)
        {
            if (profilePoints == null || pointIndex < 0 || pointIndex >= profilePoints.Count)
                return 0f;

            ProfilePoint point = profilePoints[pointIndex];
            ProfileSpanLayoutData layout = ProfileSpanLayout.Build(profileDocument);
            return layout.GetZSpan(point.ZSpan).Evaluate(point.ZT);
        }
"""
    if old_z not in text:
        raise RuntimeError("Could not find old ResolveProfilePointZ block in ShapeStamperProfileGenerator.cs")
    text = text.replace(old_z, new_z, 1)

    # remove stale helper methods that depended on profile anchors
    for snippet in [
        "        private static float ResolveDirectAnchorZ(",
        "        private static int FindPreviousDepthAnchorIndex(",
        "        private static int FindNextDepthAnchorIndex(",
        "        private static float ResolveDirectAnchorX(",
        "        private static int FindPreviousAnchorIndex(",
        "        private static int FindNextAnchorIndex(",
        "        private static float GetAuthoredMinX(",
        "        private static float GetAuthoredMaxX(",
        "        private static float GetResolvedOverallMinX(",
        "        private static float GetResolvedOverallMaxX(",
    ]:
        while snippet in text:
            start = text.index(snippet)
            next_idx = text.find("\n        private static ", start + 1)
            if next_idx == -1:
                raise RuntimeError(f"Could not safely trim stale helper starting with: {snippet}")
            text = text[:start] + text[next_idx + 1:]

    write(GENERATOR, text)
    print("Patched ShapeStamperProfileGenerator.cs")


def patch_window():
    text = read(WINDOW)

    text = text.replace(
        "        private void DrawSelectedShapeElementInspector()\n\n\n        {\n",
        "        private void DrawSelectedShapeElementInspector()\n        {\n"
    )

    text = text.replace("IList<CanvasPoint> points = profileDocument.Points;", "IList<ProfilePoint> points = profileDocument.Points;")
    text = text.replace("CanvasPoint point = points[index];", "ProfilePoint point = points[index];")

    old_label = """            EditorGUILayout.LabelField(
                $"X:{point.ProfileXAnchor}   Z:{point.ProfileZAnchor}   OffX:{point.OffsetX:0.###}   OffY:{point.OffsetY:0.###}",
                EditorStyles.miniLabel);
"""
    if old_label in text:
        text = text.replace(old_label, """            EditorGUILayout.LabelField(
                $"X:{point.XSpan}   Z:{point.ZSpan}   XT:{point.XT:0.###}   ZT:{point.ZT:0.###}",
                EditorStyles.miniLabel);
""", 1)
    else:
        text = text.replace(
            '                $"X:{point.ProfileXRegion}   Z:{point.ProfileZRegion}   XT:{point.ProfileXT:0.###}   ZT:{point.ProfileZT:0.###}",',
            '                $"X:{point.XSpan}   Z:{point.ZSpan}   XT:{point.XT:0.###}   ZT:{point.ZT:0.###}",'
        )

    old_block = """            ProfileAnchorX newXAnchor = (ProfileAnchorX)EditorGUILayout.EnumPopup("Profile X Anchor", point.ProfileXAnchor);
            ProfileDepthAnchor newZAnchor = (ProfileDepthAnchor)EditorGUILayout.EnumPopup("Profile Z Anchor", point.ProfileZAnchor);
            CanvasAnchorY newYAnchor = (CanvasAnchorY)EditorGUILayout.EnumPopup("Profile Y Anchor", point.YAnchor);
            Vector2 newPosition = EditorGUILayout.Vector2Field("Position", point.Position);

            bool canEditOffsetX = point.ProfileXAnchor != ProfileAnchorX.Floating;
            bool canEditOffsetY = point.YAnchor != CanvasAnchorY.Floating || point.ProfileZAnchor != ProfileDepthAnchor.Floating;

            EditorGUI.BeginDisabledGroup(!canEditOffsetX);
            float newOffsetX = EditorGUILayout.FloatField("Offset X", point.OffsetX);
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!canEditOffsetY);
            float newOffsetY = EditorGUILayout.FloatField("Offset Y", point.OffsetY);
            EditorGUI.EndDisabledGroup();

            if (EditorGUI.EndChangeCheck())
            {
                if (newXAnchor != point.ProfileXAnchor || newYAnchor != point.YAnchor)
                {
                    ProfileCanvasPointResolver.SetAnchorsPreservePosition(
                        ref point,
                        newXAnchor,
                        newYAnchor,
                        bounds,
                        paddingGuideX,
                        borderGuideX);
                }

                point.ProfileZAnchor = newZAnchor;

                point.Position = new Vector2(
                    Mathf.Clamp(newPosition.x, 0f, profileDocument.WorldSizeMeters.x),
                    Mathf.Clamp(newPosition.y, 0f, profileDocument.WorldSizeMeters.y));

                if (point.ProfileXAnchor != ProfileAnchorX.Floating)
                    point.OffsetX = newOffsetX;
                if (point.YAnchor != CanvasAnchorY.Floating || point.ProfileZAnchor != ProfileDepthAnchor.Floating)
                    point.OffsetY = newOffsetY;

                point.Position = ProfileCanvasPointResolver.ResolvePoint(
                    point,
                    bounds,
                    bounds,
                    paddingGuideX,
                    borderGuideX,
                    paddingGuideX,
                    borderGuideX);

                points[index] = point;
                profileDocument.MarkDirty();
                _forcePreviewRefresh = true;
                Repaint();
            }
"""
    new_block = """            ProfileXSpan newXSpan = (ProfileXSpan)EditorGUILayout.EnumPopup("Profile X Span", point.XSpan);
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

                points[index] = point;
                profileDocument.MarkDirty();
                _forcePreviewRefresh = true;
                Repaint();
            }
"""
    if old_block not in text:
        raise RuntimeError("Could not find old profile inspector block in ShapeStamperWindow.cs")
    text = text.replace(old_block, new_block, 1)

    write(WINDOW, text)
    print("Patched ShapeStamperWindow.cs")


def remove_old_anchor_files():
    removed = []
    for path in (OLD_PROFILE_ANCHOR_X, OLD_PROFILE_DEPTH_ANCHOR):
        if path.exists():
            path.unlink()
            removed.append(path.name)
    print(f"Removed old profile anchor files: {', '.join(removed) if removed else 'none found'}")


def main():
    try:
        write_new_model_files()
        patch_profile_document()
        rewrite_profile_resolver()
        patch_generator()
        patch_window()
        remove_old_anchor_files()
    except Exception as exc:
        print(f"Patch failed: {exc}", file=sys.stderr)
        return 1

    print()
    print("Done.")
    print("Next checks:")
    print("1. Let Unity recompile.")
    print("2. Reset Profile.")
    print("3. Confirm the profile inspector now edits X Span / Z Span / XT / ZT.")
    print("4. Confirm old profile anchor files are gone.")
    print("5. Fix any remaining compile errors from stale old profile-anchor references.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())