from pathlib import Path

path = Path("Assets/Nodes/Layout/Editor/ShapeStamperWindow/ShapeStamperWindow.cs")
text = path.read_text(encoding="utf-8")

text = text.replace(
    "IList<CanvasPoint> points = shapeDocument.Points;\n            int index = FindPointIndex(points, pointId);\n            if (index < 0)\n                return;\n\n            ProfilePoint point = points[index];",
    "IList<CanvasPoint> points = shapeDocument.Points;\n            int index = FindPointIndex(points, pointId);\n            if (index < 0)\n                return;\n\n            CanvasPoint point = points[index];"
)

text = text.replace(
    "IList<ProfilePoint> points = profileDocument.ProfilePoints;\n            int index = FindProfilePointIndex(points, selected.Id);\n            if (index < 0)\n                return;\n\n            CanvasPoint point = points[index];",
    "IList<ProfilePoint> points = profileDocument.ProfilePoints;\n            int index = FindProfilePointIndex(points, selected.Id);\n            if (index < 0)\n                return;\n\n            ProfilePoint point = points[index];"
)

text = text.replace(
    "int requiredSegments = Mathf.Max(0, profileDocument.Points.Count - 1);",
    "int requiredSegments = Mathf.Max(0, profileDocument.ProfilePoints.Count - 1);"
)

path.write_text(text, encoding="utf-8", newline="\n")
print("Patched ShapeStamperWindow.cs")