from pathlib import Path
import sys

ROOT = Path.cwd()

CANVAS_POINT = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Model/CanvasPoint.cs"
PROFILE_DOC = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Documents/ProfileCanvasDocument.cs"
PROFILE_RESOLVER = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Core/ProfileCanvasPointResolver.cs"
WINDOW = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/ShapeStamperWindow.cs"

PROFILE_POINT = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Model/ProfilePoint.cs"
PROFILE_X_SPAN = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Model/ProfileXSpan.cs"
PROFILE_Z_SPAN = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Model/ProfileZSpan.cs"
PROFILE_SPAN_LAYOUT = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Core/ProfileSpanLayout.cs"


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


def ensure_support_files():
    if not PROFILE_X_SPAN.exists():
        write(PROFILE_X_SPAN, """namespace DLN
{
    public enum ProfileXSpan
    {
        PaddingToContent,
        ContentToBorder
    }
}
""")

    if not PROFILE_Z_SPAN.exists():
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

    if not PROFILE_POINT.exists():
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

    if not PROFILE_SPAN_LAYOUT.exists():
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


def rewrite_canvas_point():
    write(CANVAS_POINT, """using System;
using UnityEngine;

namespace DLN
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
    print("Rewrote CanvasPoint.cs")


def patch_profile_document():
    text = read(PROFILE_DOC)

    text = text.replace(
        "[SerializeField] private List<CanvasPoint> points = new();",
        "[SerializeField] private List<ProfilePoint> points = new();"
    )

    text = text.replace(
        "public IList<CanvasPoint> Points => points;",
        "public IReadOnlyList<ProfilePoint> Points => points;\\n        public IList<ProfilePoint> MutablePoints => points;"
    )

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
    text = replace_or_fail(text, old_default, new_default, PROFILE_DOC.name)

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


def patch_window():
    text = read(WINDOW)

    text = text.replace(
        "        private void DrawSelectedShapeElementInspector()\\n\\n\\n        {\\n",
        "        private void DrawSelectedShapeElementInspector()\\n        {\\n"
    )

    text = text.replace("IList<CanvasPoint> points = profileDocument.Points;", "IList<ProfilePoint> points = profileDocument.MutablePoints;")
    text = text.replace("CanvasPoint point = points[index];", "ProfilePoint point = points[index];")

    old_label = """            EditorGUILayout.LabelField($\"Profile Point {point.Id}\", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
"""
    new_label = """            EditorGUILayout.LabelField($\"Profile Point {point.Id}\", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                $\"X:{point.XSpan}   Z:{point.ZSpan}   XT:{point.XT:0.###}   ZT:{point.ZT:0.###}\",
                EditorStyles.miniLabel);

            EditorGUI.BeginChangeCheck();
"""
    text = replace_or_fail(text, old_label, new_label, WINDOW.name)

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
    text = replace_or_fail(text, old_block, new_block, WINDOW.name)

    write(WINDOW, text)
    print("Patched ShapeStamperWindow.cs")


def main():
    try:
        ensure_support_files()
        rewrite_canvas_point()
        patch_profile_document()
        rewrite_profile_resolver()
        patch_window()
    except Exception as exc:
        print(f"Repair failed: {exc}", file=sys.stderr)
        return 1

    print()
    print("Done.")
    print("Let Unity recompile, then check the next error cluster.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())