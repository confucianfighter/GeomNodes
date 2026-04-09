#!/usr/bin/env python3
from pathlib import Path
import argparse
import sys

FILES = {
    "CanvasAnchorX.cs": """using System;

namespace DLN
{
    [Serializable]
    public enum CanvasAnchorX
    {
        None = 0,
        Left = 1,
        Center = 2,
        Right = 3
    }
}
""",
    "CanvasAnchorY.cs": """using System;

namespace DLN
{
    [Serializable]
    public enum CanvasAnchorY
    {
        None = 0,
        Bottom = 1,
        Center = 2,
        Top = 3
    }
}
""",
    "CanvasPoint.cs": """using System;
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

            XAnchor = CanvasAnchorX.None;
            YAnchor = CanvasAnchorY.None;

            OffsetX = 0f;
            OffsetY = 0f;
        }
    }
}
""",
    "ShapeCanvasPointResolver.cs": """using UnityEngine;

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
""",
    "ShapeCanvasConstraintUtility.cs": """using UnityEngine;

namespace DLN
{
    public static class ShapeCanvasConstraintUtility
    {
        public static void AssignAnchorsFromCurrentPosition(
            ref CanvasPoint point,
            Rect bounds,
            CanvasAnchorX xAnchor,
            CanvasAnchorY yAnchor)
        {
            point.XAnchor = xAnchor;
            point.YAnchor = yAnchor;
            RecalculateOffsetsFromPosition(ref point, bounds);
        }

        public static void RecalculateOffsetsFromPosition(
            ref CanvasPoint point,
            Rect bounds)
        {
            point.OffsetX = point.XAnchor switch
            {
                CanvasAnchorX.Left => point.Position.x - bounds.xMin,
                CanvasAnchorX.Center => point.Position.x - bounds.center.x,
                CanvasAnchorX.Right => point.Position.x - bounds.xMax,
                _ => point.OffsetX
            };

            point.OffsetY = point.YAnchor switch
            {
                CanvasAnchorY.Bottom => point.Position.y - bounds.yMin,
                CanvasAnchorY.Center => point.Position.y - bounds.center.y,
                CanvasAnchorY.Top => point.Position.y - bounds.yMax,
                _ => point.OffsetY
            };
        }

        public static void SetPointPosition(
            ref CanvasPoint point,
            Vector2 newPosition,
            Rect bounds)
        {
            point.Position = newPosition;
            RecalculateOffsetsFromPosition(ref point, bounds);
        }

        public static void ResolvePointIntoPosition(
            ref CanvasPoint point,
            Rect oldBounds,
            Rect newBounds)
        {
            point.Position = ShapeCanvasPointResolver.ResolvePoint(point, oldBounds, newBounds);
            RecalculateOffsetsFromPosition(ref point, newBounds);
        }

        public static void ResolveAllPointsIntoPosition(
            CanvasPoint[] points,
            Rect oldBounds,
            Rect newBounds)
        {
            if (points == null)
                return;

            for (int i = 0; i < points.Length; i++)
            {
                var point = points[i];
                ResolvePointIntoPosition(ref point, oldBounds, newBounds);
                points[i] = point;
            }
        }
    }
}
"""
}

def write_text(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8", newline="\n")
    print(f"[write] {path}")

def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("repo_root", help="Path to the GeomNodes repo root")
    args = parser.parse_args()

    root = Path(args.repo_root).expanduser().resolve()
    if not root.exists():
        print(f"Repo root does not exist: {root}", file=sys.stderr)
        return 1

    model_dir = root / "Assets" / "Nodes" / "Layout" / "Editor" / "ShapeStamperWindow" / "Canvas" / "Model"
    core_dir = root / "Assets" / "Nodes" / "Layout" / "Editor" / "ShapeStamperWindow" / "Canvas" / "Core"

    write_text(model_dir / "CanvasAnchorX.cs", FILES["CanvasAnchorX.cs"])
    write_text(model_dir / "CanvasAnchorY.cs", FILES["CanvasAnchorY.cs"])
    write_text(model_dir / "CanvasPoint.cs", FILES["CanvasPoint.cs"])
    write_text(core_dir / "ShapeCanvasPointResolver.cs", FILES["ShapeCanvasPointResolver.cs"])
    write_text(core_dir / "ShapeCanvasConstraintUtility.cs", FILES["ShapeCanvasConstraintUtility.cs"])

    print("\\nDone.")
    print("CanvasPoint now uses:")
    print("  .Id, .Position, .XAnchor, .YAnchor, .OffsetX, .OffsetY")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())