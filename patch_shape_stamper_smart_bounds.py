from pathlib import Path
import sys

ROOT = Path.cwd()

PROFILE_DOC = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Documents/ProfileCanvasDocument.cs"
GENERATOR = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/ShapeStamperProfileGenerator.cs"
WINDOW = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/ShapeStamperWindow.cs"


def read(path: Path) -> str:
    if not path.exists():
        raise FileNotFoundError(f"Missing file: {path}")
    return path.read_text(encoding="utf-8")


def write(path: Path, text: str) -> None:
    path.write_text(text, encoding="utf-8", newline="\n")


def replace_once(text: str, old: str, new: str, where: str) -> str:
    if old not in text:
        raise RuntimeError(f"Could not find expected block in {where}:\n{old[:240]}...")
    return text.replace(old, new, 1)


def patch_profile_document():
    text = read(PROFILE_DOC)

    old = """            points.Add(new CanvasPoint { Id = 0, Position = new Vector2(0.00f, 0.10f), ProfileXAnchor = ProfileAnchorX.Padding, YAnchor = CanvasAnchorY.Top });
            points.Add(new CanvasPoint { Id = 1, Position = new Vector2(0.08f, 0.25f), ProfileXAnchor = ProfileAnchorX.Content, YAnchor = CanvasAnchorY.Floating });
            points.Add(new CanvasPoint { Id = 2, Position = new Vector2(0.16f, 0.55f), ProfileXAnchor = ProfileAnchorX.Border, YAnchor = CanvasAnchorY.Bottom });
"""
    new = """            points.Add(new CanvasPoint
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
    text = replace_once(text, old, new, PROFILE_DOC.name)
    write(PROFILE_DOC, text)
    print("Patched ProfileCanvasDocument.cs")


def patch_generator():
    text = read(GENERATOR)

    old = """        private static float ResolveDirectAnchorZ(CanvasPoint point, ProfileCanvasDocument profileDocument)
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
"""
    new = """        private static float ResolveDirectAnchorZ(CanvasPoint point, ProfileCanvasDocument profileDocument)
        {
            float raw;
            switch (point.ProfileZAnchor)
            {
                case ProfileDepthAnchor.Padding:
                    raw = point.OffsetY;
                    break;

                case ProfileDepthAnchor.Content:
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
"""
    text = replace_once(text, old, new, GENERATOR.name)
    write(GENERATOR, text)
    print("Patched ShapeStamperProfileGenerator.cs")


def patch_window():
    text = read(WINDOW)

    # Clean the weird blank lines if still present.
    text = text.replace(
        "        private void DrawSelectedShapeElementInspector()\n\n\n        {\n",
        "        private void DrawSelectedShapeElementInspector()\n        {\n"
    )

    old_assignment = """                if (point.ProfileXAnchor != ProfileAnchorX.Floating)
                    point.OffsetX = newOffsetX;
                if (point.YAnchor != CanvasAnchorY.Floating)
                    point.OffsetY = newOffsetY;
"""
    new_assignment = """                if (point.ProfileXAnchor != ProfileAnchorX.Floating)
                    point.OffsetX = newOffsetX;
                if (point.YAnchor != CanvasAnchorY.Floating || point.ProfileZAnchor != ProfileDepthAnchor.Floating)
                    point.OffsetY = newOffsetY;
"""
    text = replace_once(text, old_assignment, new_assignment, WINDOW.name)

    old_label = """            EditorGUILayout.LabelField($\"Profile Point {point.Id}\", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
"""
    new_label = """            EditorGUILayout.LabelField($\"Profile Point {point.Id}\", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                $\"X:{point.ProfileXAnchor}   Z:{point.ProfileZAnchor}   OffX:{point.OffsetX:0.###}   OffY:{point.OffsetY:0.###}\",
                EditorStyles.miniLabel);

            EditorGUI.BeginChangeCheck();
"""
    text = replace_once(text, old_label, new_label, WINDOW.name)

    write(WINDOW, text)
    print("Patched ShapeStamperWindow.cs")


def main():
    try:
        patch_profile_document()
        patch_generator()
        patch_window()
    except Exception as exc:
        print(f"Patch failed: {exc}", file=sys.stderr)
        return 1

    print()
    print("Done.")
    print("Next checks:")
    print("1. Let Unity recompile.")
    print("2. Reset Profile.")
    print("3. Confirm the three default profile points are now:")
    print("   padding/padding, content/content, border/border")
    print("4. Set front padding and front border to the same value.")
    print("5. Verify the three anchors now land at base, padding, and padding+border respectively.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())