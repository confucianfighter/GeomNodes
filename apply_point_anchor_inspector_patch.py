#!/usr/bin/env python3
"""
Apply the Shape Stamper point-anchor / WYSIWYG offset patches.

Run from the repo root, e.g.
    python apply_point_anchor_inspector_patch.py
"""

from __future__ import annotations

from pathlib import Path
import re
import sys


ROOT = Path.cwd()


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def write(path: Path, text: str) -> None:
    path.write_text(text, encoding="utf-8", newline="\n")


def require_replace(text: str, old: str, new: str, file_label: str, count: int = 1) -> str:
    occurrences = text.count(old)
    if occurrences < count:
        raise RuntimeError(
            f"{file_label}: expected at least {count} occurrence(s) of target snippet, found {occurrences}."
        )
    return text.replace(old, new, count)


def patch_shape_canvas_point_resolver(root: Path) -> None:
    path = root / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Core/ShapeCanvasPointResolver.cs"
    new_text = """using UnityEngine;

namespace DLN
{
    public static class ShapeCanvasPointResolver
    {
        public static Vector2 ResolvePoint(
            CanvasPoint point,
            Rect oldBounds,
            Rect newBounds)
        {
            float x = ResolveX(point, oldBounds, newBounds);
            float y = ResolveY(point, oldBounds, newBounds);
            return new Vector2(x, y);
        }

        public static void RecalculateOffsets(ref CanvasPoint point, Rect bounds)
        {
            point.OffsetX = CalculateOffsetX(point, bounds);
            point.OffsetY = CalculateOffsetY(point, bounds);
        }

        public static void SetAnchorsPreservePosition(
            ref CanvasPoint point,
            CanvasAnchorX xAnchor,
            CanvasAnchorY yAnchor,
            Rect bounds)
        {
            point.XAnchor = xAnchor;
            point.YAnchor = yAnchor;
            RecalculateOffsets(ref point, bounds);
        }

        public static void ResizePointPreservingBehavior(
            ref CanvasPoint point,
            Rect oldBounds,
            Rect newBounds)
        {
            point.Position = ResolvePoint(point, oldBounds, newBounds);
            RecalculateOffsets(ref point, newBounds);
        }

        public static float ResolveX(CanvasPoint point, Rect oldBounds, Rect newBounds)
        {
            switch (point.XAnchor)
            {
                case CanvasAnchorX.Left:
                    return newBounds.xMin + point.OffsetX;

                case CanvasAnchorX.Center:
                    return newBounds.center.x + point.OffsetX;

                case CanvasAnchorX.Right:
                    return newBounds.xMax + point.OffsetX;

                case CanvasAnchorX.None:
                default:
                    return RemapPreservingRatio(
                        point.Position.x,
                        oldBounds.xMin,
                        oldBounds.xMax,
                        newBounds.xMin,
                        newBounds.xMax);
            }
        }

        public static float ResolveY(CanvasPoint point, Rect oldBounds, Rect newBounds)
        {
            switch (point.YAnchor)
            {
                case CanvasAnchorY.Bottom:
                    return newBounds.yMin + point.OffsetY;

                case CanvasAnchorY.Center:
                    return newBounds.center.y + point.OffsetY;

                case CanvasAnchorY.Top:
                    return newBounds.yMax + point.OffsetY;

                case CanvasAnchorY.None:
                default:
                    return RemapPreservingRatio(
                        point.Position.y,
                        oldBounds.yMin,
                        oldBounds.yMax,
                        newBounds.yMin,
                        newBounds.yMax);
            }
        }

        public static float CalculateOffsetX(CanvasPoint point, Rect bounds)
        {
            switch (point.XAnchor)
            {
                case CanvasAnchorX.Left:
                    return point.Position.x - bounds.xMin;

                case CanvasAnchorX.Center:
                    return point.Position.x - bounds.center.x;

                case CanvasAnchorX.Right:
                    return point.Position.x - bounds.xMax;

                case CanvasAnchorX.None:
                default:
                    return 0f;
            }
        }

        public static float CalculateOffsetY(CanvasPoint point, Rect bounds)
        {
            switch (point.YAnchor)
            {
                case CanvasAnchorY.Bottom:
                    return point.Position.y - bounds.yMin;

                case CanvasAnchorY.Center:
                    return point.Position.y - bounds.center.y;

                case CanvasAnchorY.Top:
                    return point.Position.y - bounds.yMax;

                case CanvasAnchorY.None:
                default:
                    return 0f;
            }
        }

        private static float RemapPreservingRatio(
            float value,
            float oldMin,
            float oldMax,
            float newMin,
            float newMax)
        {
            float oldSize = oldMax - oldMin;
            if (Mathf.Abs(oldSize) < 0.0001f)
                return newMin;

            float t = (value - oldMin) / oldSize;
            return Mathf.Lerp(newMin, newMax, t);
        }
    }
}
"""
    write(path, new_text)


def patch_editor_canvas(root: Path) -> None:
    path = root / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Core/EditorCanvas.cs"
    text = read(path)

    old = """        private void SetPointPosition(int pointId, Vector2 position)
        {
            for (int i = 0; i < Document.Points.Count; i++)
            {
                if (Document.Points[i].Id != pointId)
                    continue;

                CanvasPoint p = Document.Points[i];
                p.Position = position;
                Document.Points[i] = p;
                return;
            }
        }
"""
    new = """        private void SetPointPosition(int pointId, Vector2 position)
        {
            Rect bounds = CanvasMath.GetWorldRect(Document);

            for (int i = 0; i < Document.Points.Count; i++)
            {
                if (Document.Points[i].Id != pointId)
                    continue;

                CanvasPoint p = Document.Points[i];
                p.Position = position;
                ShapeCanvasPointResolver.RecalculateOffsets(ref p, bounds);
                Document.Points[i] = p;
                return;
            }
        }
"""
    text = require_replace(text, old, new, path.as_posix())
    write(path, text)


def patch_shape_canvas_document(root: Path) -> None:
    path = root / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Documents/ShapeCanvasDocument.cs"
    text = read(path)

    insert_after = """        public Rect GetCanvasFrameRect()
        {
            return new Rect(0f, 0f, WorldSizeMeters.x, WorldSizeMeters.y);
        }

"""
    addition = """        public void ResizeWorld(Vector2 newSize)
        {
            Rect oldBounds = GetCanvasFrameRect();

            WorldSizeMeters = new Vector2(
                Mathf.Max(0.0001f, newSize.x),
                Mathf.Max(0.0001f, newSize.y)
            );

            Rect newBounds = GetCanvasFrameRect();

            ResizePointList(points, oldBounds, newBounds);
            ResizePointList(innerPoints, oldBounds, newBounds);
        }

        private static void ResizePointList(List<CanvasPoint> list, Rect oldBounds, Rect newBounds)
        {
            if (list == null)
                return;

            for (int i = 0; i < list.Count; i++)
            {
                CanvasPoint p = list[i];
                ShapeCanvasPointResolver.ResizePointPreservingBehavior(ref p, oldBounds, newBounds);
                list[i] = p;
            }
        }

"""
    text = require_replace(text, insert_after, insert_after + addition, path.as_posix())
    write(path, text)


def patch_shape_stamper_window(root: Path) -> None:
    path = root / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/ShapeStamperWindow.cs"
    text = read(path)

    old_world_assign = """            shapeDocument.WorldSizeMeters = new Vector2(
                Mathf.Max(0.0001f, newShapeWidth),
                Mathf.Max(0.0001f, newShapeHeight)
            );

"""
    new_world_assign = """            Vector2 requestedShapeSize = new Vector2(
                Mathf.Max(0.0001f, newShapeWidth),
                Mathf.Max(0.0001f, newShapeHeight)
            );

            if (requestedShapeSize != shapeDocument.WorldSizeMeters)
                shapeDocument.ResizeWorld(requestedShapeSize);

"""
    text = require_replace(text, old_world_assign, new_world_assign, path.as_posix())

    old_profile_assign = """            profileDocument.WorldSizeMeters = new Vector2(
                Mathf.Max(0.0001f, newProfileWidth),
                Mathf.Max(0.0001f, newProfileHeight)
            );

"""
    new_profile_assign = """            profileDocument.WorldSizeMeters = new Vector2(
                Mathf.Max(0.0001f, newProfileWidth),
                Mathf.Max(0.0001f, newProfileHeight)
            );

            DrawSelectedShapePointInspector();

"""
    text = require_replace(text, old_profile_assign, new_profile_assign, path.as_posix())

    insert_before = """        private void DrawMaterialSettings()
"""
    inspector = """        private void DrawSelectedShapePointInspector()
        {
            if (shapeSelection == null || shapeSelection.Count != 1)
                return;

            CanvasElementRef selected = default;
            foreach (CanvasElementRef element in shapeSelection.Elements)
            {
                selected = element;
                break;
            }

            if (!selected.IsPoint)
                return;

            IList<CanvasPoint> points = shapeDocument.Points;
            int index = -1;
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].Id == selected.Id)
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
                return;

            CanvasPoint point = points[index];
            Rect bounds = shapeDocument.GetCanvasFrameRect();

            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Point {point.Id}", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            CanvasAnchorX newXAnchor = (CanvasAnchorX)EditorGUILayout.EnumPopup("X Anchor", point.XAnchor);
            CanvasAnchorY newYAnchor = (CanvasAnchorY)EditorGUILayout.EnumPopup("Y Anchor", point.YAnchor);

            EditorGUILayout.LabelField("Position", $"{point.Position.x:0.###}, {point.Position.y:0.###}");
            EditorGUILayout.LabelField("Offset", $"{point.OffsetX:0.###}, {point.OffsetY:0.###}");

            if (EditorGUI.EndChangeCheck())
            {
                ShapeCanvasPointResolver.SetAnchorsPreservePosition(ref point, newXAnchor, newYAnchor, bounds);
                points[index] = point;
                shapeDocument.MarkDirty();
                Repaint();
            }

            EditorGUILayout.EndVertical();
        }

"""
    text = require_replace(text, insert_before, inspector + insert_before, path.as_posix())

    write(path, text)


def patch_canvas_anchor_enums(root: Path) -> None:
    # Optional clarity rename: None -> Floating in the enum declarations and use sites.
    x_path = root / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Model/CanvasAnchorX.cs"
    y_path = root / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Model/CanvasAnchorY.cs"
    point_path = root / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Model/CanvasPoint.cs"
    resolver_path = root / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Core/ShapeCanvasPointResolver.cs"

    x_text = read(x_path).replace("        None = 0,", "        Floating = 0,")
    y_text = read(y_path).replace("        None = 0,", "        Floating = 0,")

    point_text = read(point_path)
    point_text = point_text.replace("            XAnchor = CanvasAnchorX.None;", "            XAnchor = CanvasAnchorX.Floating;")
    point_text = point_text.replace("            YAnchor = CanvasAnchorY.None;", "            YAnchor = CanvasAnchorY.Floating;")

    resolver_text = read(resolver_path)
    resolver_text = resolver_text.replace("case CanvasAnchorX.None:", "case CanvasAnchorX.Floating:")
    resolver_text = resolver_text.replace("case CanvasAnchorY.None:", "case CanvasAnchorY.Floating:")

    write(x_path, x_text)
    write(y_path, y_text)
    write(point_path, point_text)
    write(resolver_path, resolver_text)


def main() -> int:
    root = ROOT

    required = [
        root / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Core/ShapeCanvasPointResolver.cs",
        root / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Core/EditorCanvas.cs",
        root / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Documents/ShapeCanvasDocument.cs",
        root / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/ShapeStamperWindow.cs",
    ]

    missing = [p.as_posix() for p in required if not p.exists()]
    if missing:
        print("Missing expected files:")
        for item in missing:
            print(f"  - {item}")
        return 1

    patch_shape_canvas_point_resolver(root)
    patch_editor_canvas(root)
    patch_shape_canvas_document(root)
    patch_shape_stamper_window(root)
    patch_canvas_anchor_enums(root)

    print("Applied point-anchor / WYSIWYG offset patches successfully.")
    print("Next: let Unity recompile, then test dragging a point, changing anchors, and resizing shape world width/height.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
