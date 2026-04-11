#!/usr/bin/env python3
from pathlib import Path

ROOT = Path.cwd()

def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")

def write(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8", newline="\n")

def replace_once(text: str, old: str, new: str, label: str) -> str:
    if old not in text:
        raise RuntimeError(f"{label}: target snippet not found")
    return text.replace(old, new, 1)

def main() -> int:
    profile_depth_anchor_path = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Model/ProfileDepthAnchor.cs"
    canvas_point_path = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Model/CanvasPoint.cs"
    profile_doc_path = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Documents/ProfileCanvasDocument.cs"
    window_path = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/ShapeStamperWindow.cs"
    generator_path = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/ShapeStamperProfileGenerator.cs"

    for path in [canvas_point_path, profile_doc_path, window_path, generator_path]:
        if not path.exists():
            print(f"Missing expected file: {path}")
            return 1

    write(
        profile_depth_anchor_path,
        '''using System;

namespace DLN
{
    [Serializable]
    public enum ProfileDepthAnchor
    {
        Floating = 0,
        Content = 1,
        Padding = 2,
        Border = 3
    }
}
''',
    )

    canvas_point = read(canvas_point_path)
    label = str(canvas_point_path)

    canvas_point = replace_once(
        canvas_point,
        '''        public CanvasAnchorX XAnchor;
        public CanvasAnchorY YAnchor;
        public ProfileAnchorX ProfileXAnchor;

        public float OffsetX;
        public float OffsetY;
''',
        '''        public CanvasAnchorX XAnchor;
        public CanvasAnchorY YAnchor;
        public ProfileAnchorX ProfileXAnchor;
        public ProfileDepthAnchor ProfileZAnchor;

        public float OffsetX;
        public float OffsetY;
''',
        label,
    )

    canvas_point = replace_once(
        canvas_point,
        '''            XAnchor = CanvasAnchorX.Floating;
            YAnchor = CanvasAnchorY.Floating;
            ProfileXAnchor = ProfileAnchorX.Floating;

            OffsetX = 0f;
            OffsetY = 0f;
''',
        '''            XAnchor = CanvasAnchorX.Floating;
            YAnchor = CanvasAnchorY.Floating;
            ProfileXAnchor = ProfileAnchorX.Floating;
            ProfileZAnchor = ProfileDepthAnchor.Floating;

            OffsetX = 0f;
            OffsetY = 0f;
''',
        label,
    )
    write(canvas_point_path, canvas_point)

    profile_doc = read(profile_doc_path)
    label = str(profile_doc_path)

    profile_doc = replace_once(
        profile_doc,
        '''        [SerializeField] private float leftBorder;
        [SerializeField] private float rightBorder;
        [SerializeField] private float topBorder;
        [SerializeField] private float bottomBorder;

        [SerializeField, HideInInspector] private int revision;
''',
        '''        [SerializeField] private float leftBorder;
        [SerializeField] private float rightBorder;
        [SerializeField] private float topBorder;
        [SerializeField] private float bottomBorder;

        [SerializeField] private float frontPaddingDepth;
        [SerializeField] private float frontBorderDepth;

        [SerializeField, HideInInspector] private int revision;
''',
        label,
    )

    profile_doc = replace_once(
        profile_doc,
        '''        public float LeftBorder { get => leftBorder; set => leftBorder = Mathf.Max(0f, value); }
        public float RightBorder { get => rightBorder; set => rightBorder = Mathf.Max(0f, value); }
        public float TopBorder { get => topBorder; set => topBorder = Mathf.Max(0f, value); }
        public float BottomBorder { get => bottomBorder; set => bottomBorder = Mathf.Max(0f, value); }

        public float AveragePadding => (leftPadding + rightPadding + topPadding + bottomPadding) * 0.25f;
''',
        '''        public float LeftBorder { get => leftBorder; set => leftBorder = Mathf.Max(0f, value); }
        public float RightBorder { get => rightBorder; set => rightBorder = Mathf.Max(0f, value); }
        public float TopBorder { get => topBorder; set => topBorder = Mathf.Max(0f, value); }
        public float BottomBorder { get => bottomBorder; set => bottomBorder = Mathf.Max(0f, value); }

        public float FrontPaddingDepth { get => frontPaddingDepth; set => frontPaddingDepth = Mathf.Max(0f, value); }
        public float FrontBorderDepth { get => frontBorderDepth; set => frontBorderDepth = Mathf.Max(0f, value); }

        public float AveragePadding => (leftPadding + rightPadding + topPadding + bottomPadding) * 0.25f;
''',
        label,
    )
    write(profile_doc_path, profile_doc)

    window = read(window_path)
    label = str(window_path)

    window = replace_once(
        window,
        '''            EditorGUILayout.LabelField(
                $"Padding Guide X: {profileDocument.PaddingGuideX:0.###}   Border Guide X: {profileDocument.BorderGuideX:0.###}",
                EditorStyles.miniLabel);

            bool changed =
''',
        '''            EditorGUILayout.LabelField(
                $"Padding Guide X: {profileDocument.PaddingGuideX:0.###}   Border Guide X: {profileDocument.BorderGuideX:0.###}",
                EditorStyles.miniLabel);

            float newFrontPaddingDepth = EditorGUILayout.FloatField("Front Padding Depth", profileDocument.FrontPaddingDepth);
            float newFrontBorderDepth = EditorGUILayout.FloatField("Front Border Depth", profileDocument.FrontBorderDepth);

            bool changed =
''',
        label,
    )

    window = replace_once(
        window,
        '''                !Mathf.Approximately(newLeftBorder, profileDocument.LeftBorder) ||
                !Mathf.Approximately(newRightBorder, profileDocument.RightBorder) ||
                !Mathf.Approximately(newTopBorder, profileDocument.TopBorder) ||
                !Mathf.Approximately(newBottomBorder, profileDocument.BottomBorder);

            if (changed)
            {
                profileDocument.SetGuideValues(
                    newLeftPadding,
                    newRightPadding,
                    newTopPadding,
                    newBottomPadding,
                    newLeftBorder,
                    newRightBorder,
                    newTopBorder,
                    newBottomBorder);
                _forcePreviewRefresh = true;
            }
''',
        '''                !Mathf.Approximately(newLeftBorder, profileDocument.LeftBorder) ||
                !Mathf.Approximately(newRightBorder, profileDocument.RightBorder) ||
                !Mathf.Approximately(newTopBorder, profileDocument.TopBorder) ||
                !Mathf.Approximately(newBottomBorder, profileDocument.BottomBorder) ||
                !Mathf.Approximately(newFrontPaddingDepth, profileDocument.FrontPaddingDepth) ||
                !Mathf.Approximately(newFrontBorderDepth, profileDocument.FrontBorderDepth);

            if (changed)
            {
                profileDocument.SetGuideValues(
                    newLeftPadding,
                    newRightPadding,
                    newTopPadding,
                    newBottomPadding,
                    newLeftBorder,
                    newRightBorder,
                    newTopBorder,
                    newBottomBorder);

                profileDocument.FrontPaddingDepth = newFrontPaddingDepth;
                profileDocument.FrontBorderDepth = newFrontBorderDepth;
                profileDocument.MarkDirty();
                _forcePreviewRefresh = true;
            }
''',
        label,
    )

    window = replace_once(
        window,
        '''            ProfileAnchorX newXAnchor = (ProfileAnchorX)EditorGUILayout.EnumPopup("Profile X Anchor", point.ProfileXAnchor);
            CanvasAnchorY newYAnchor = (CanvasAnchorY)EditorGUILayout.EnumPopup("Profile Y Anchor", point.YAnchor);
            Vector2 newPosition = EditorGUILayout.Vector2Field("Position", point.Position);

            bool canEditOffsetX = point.ProfileXAnchor != ProfileAnchorX.Floating;
            bool canEditOffsetY = point.YAnchor != CanvasAnchorY.Floating;
''',
        '''            ProfileAnchorX newXAnchor = (ProfileAnchorX)EditorGUILayout.EnumPopup("Profile X Anchor", point.ProfileXAnchor);
            ProfileDepthAnchor newZAnchor = (ProfileDepthAnchor)EditorGUILayout.EnumPopup("Profile Z Anchor", point.ProfileZAnchor);
            CanvasAnchorY newYAnchor = (CanvasAnchorY)EditorGUILayout.EnumPopup("Profile Y Anchor", point.YAnchor);
            Vector2 newPosition = EditorGUILayout.Vector2Field("Position", point.Position);

            bool canEditOffsetX = point.ProfileXAnchor != ProfileAnchorX.Floating;
            bool canEditOffsetY = point.YAnchor != CanvasAnchorY.Floating || point.ProfileZAnchor != ProfileDepthAnchor.Floating;
''',
        label,
    )

    window = replace_once(
        window,
        '''                if (newXAnchor != point.ProfileXAnchor || newYAnchor != point.YAnchor)
                {
                    ProfileCanvasPointResolver.SetAnchorsPreservePosition(
                        ref point,
                        newXAnchor,
                        newYAnchor,
                        bounds,
                        paddingGuideX,
                        borderGuideX);
                }
''',
        '''                if (newXAnchor != point.ProfileXAnchor || newYAnchor != point.YAnchor)
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
''',
        label,
    )
    write(window_path, window)

    generator = read(generator_path)
    label = str(generator_path)

    generator = replace_once(
        generator,
        '''                ProfileSample sample = new ProfileSample
                {
                    Index = i,
                    Offset = p.Position.x,
                    Z = p.Position.y,
                    Point = p
                };
''',
        '''                ProfileSample sample = new ProfileSample
                {
                    Index = i,
                    Offset = p.Position.x,
                    Z = ResolveProfilePointZ(i, profileDocument.Points, profileDocument),
                    Point = p
                };
''',
        label,
    )

    anchor = '''        private static float ResolveProfilePointX(int pointIndex, IList<CanvasPoint> profilePoints, EdgeProfileDrive drive)
'''
    insert = '''        private static float ResolveProfilePointZ(int pointIndex, IList<CanvasPoint> profilePoints, ProfileCanvasDocument profileDocument)
        {
            if (profilePoints == null || pointIndex < 0 || pointIndex >= profilePoints.Count)
                return 0f;

            CanvasPoint point = profilePoints[pointIndex];
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

        private static float ResolveDirectAnchorZ(CanvasPoint point, ProfileCanvasDocument profileDocument)
        {
            float raw;
            switch (point.ProfileZAnchor)
            {
                case ProfileDepthAnchor.Content:
                    raw = point.OffsetY;
                    break;

                case ProfileDepthAnchor.Padding:
                    raw = profileDocument.FrontPaddingDepth + point.OffsetY;
                    break;

                case ProfileDepthAnchor.Border:
                    raw = profileDocument.FrontPaddingDepth + profileDocument.FrontBorderDepth + point.OffsetY;
                    break;

                case ProfileDepthAnchor.Floating:
                default:
                    raw = point.Position.y;
                    break;
            }

            return Mathf.Max(0f, SafeFinite(raw, 0f));
        }

        private static int FindPreviousDepthAnchorIndex(IList<CanvasPoint> profilePoints, int fromIndex)
        {
            for (int i = fromIndex - 1; i >= 0; i--)
            {
                if (profilePoints[i].ProfileZAnchor != ProfileDepthAnchor.Floating)
                    return i;
            }

            return -1;
        }

        private static int FindNextDepthAnchorIndex(IList<CanvasPoint> profilePoints, int fromIndex)
        {
            for (int i = fromIndex + 1; i < profilePoints.Count; i++)
            {
                if (profilePoints[i].ProfileZAnchor != ProfileDepthAnchor.Floating)
                    return i;
            }

            return -1;
        }

'''
    generator = replace_once(generator, anchor, insert + anchor, label)
    write(generator_path, generator)

    print("Patched Shape Stamper with front depth-only profile Z anchors.")
    print("Files updated:")
    print(f"  {canvas_point_path}")
    print(f"  {profile_doc_path}")
    print(f"  {window_path}")
    print(f"  {generator_path}")
    print("New file:")
    print(f"  {profile_depth_anchor_path}")

    return 0

if __name__ == "__main__":
    raise SystemExit(main())
