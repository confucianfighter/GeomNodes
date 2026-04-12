from pathlib import Path

ROOT = Path.cwd()

WINDOW = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/ShapeStamperWindow.cs"
RESOLVER = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Core/ProfileCanvasPointResolver.cs"

w = WINDOW.read_text(encoding="utf-8")
r = RESOLVER.read_text(encoding="utf-8")

w = w.replace(
    "            ProfilePoint point = points[index];",
    "            CanvasPoint point = points[index];"
)

w = w.replace(
    "            int requiredSegments = Mathf.Max(0, profileDocument.Points.Count - 1);",
    "            int requiredSegments = Mathf.Max(0, profileDocument.ProfilePoints.Count - 1);"
)

w = w.replace("using DLN;\n", "")

r = r.replace(
    """            doc.SetGuideValues(
                leftPadding: paddingGuideX,
                rightPadding: paddingGuideX,
                topPadding: paddingGuideX,
                bottomPadding: paddingGuideX,
                leftBorder: borderOnly,
                rightBorder: borderOnly,
                topBorder: borderOnly,
                bottomBorder: borderOnly);""",
    """            doc.SetGuideValues(
                paddingGuideX,
                paddingGuideX,
                paddingGuideX,
                paddingGuideX,
                borderOnly,
                borderOnly,
                borderOnly,
                borderOnly);"""
)

WINDOW.write_text(w, encoding="utf-8", newline="\n")
RESOLVER.write_text(r, encoding="utf-8", newline="\n")

print("Patched ShapeStamperWindow.cs and ProfileCanvasPointResolver.cs")