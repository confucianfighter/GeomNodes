#!/usr/bin/env python3
\
"""
Phase 1 migration for ShapeStamper canvas points:
- Introduces shape/profile region enums
- Rewrites CanvasPoint.cs and ProfilePoint.cs to use region + lerp storage
- Rewrites ProfileXRegion.cs and ProfileZRegion.cs to the new region model
- Optionally archives legacy anchor enum files

This is intentionally conservative:
- It targets only the model folder
- It creates a timestamped backup of any changed files
- It does NOT touch resolvers, policies, drawing, or editor interaction yet

Usage:
    python phase1_region_model_migration.py /path/to/GeomNodes
    python phase1_region_model_migration.py /path/to/GeomNodes --dry-run
    python phase1_region_model_migration.py /path/to/GeomNodes --archive-anchors
"""

from __future__ import annotations

import argparse
import datetime as _dt
import shutil
import sys
from pathlib import Path


MODEL_REL = Path(
    "Assets/Nodes/Layout/Editor/ShapeStamperWindow/Canvas/Model"
)


def write_text(path: Path, text: str, dry_run: bool) -> None:
    if dry_run:
        print(f"[dry-run] write {path}")
        return
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8", newline="\n")
    print(f"[write] {path}")


def backup_file(src: Path, backup_root: Path, repo_root: Path, dry_run: bool) -> None:
    if not src.exists():
        return
    rel = src.relative_to(repo_root)
    dst = backup_root / rel
    if dry_run:
        print(f"[dry-run] backup {src} -> {dst}")
        return
    dst.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(src, dst)
    print(f"[backup] {src} -> {dst}")


def archive_file(src: Path, archive_dir: Path, dry_run: bool) -> None:
    if not src.exists():
        return
    dst = archive_dir / src.name
    if dry_run:
        print(f"[dry-run] archive {src} -> {dst}")
        return
    archive_dir.mkdir(parents=True, exist_ok=True)
    shutil.move(str(src), str(dst))
    print(f"[archive] {src} -> {dst}")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("repo_root", help="Path to the Unity repo root")
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument(
        "--archive-anchors",
        action="store_true",
        help="Move CanvasAnchorX.cs and CanvasAnchorY.cs into a legacy backup folder",
    )
    args = parser.parse_args()

    repo_root = Path(args.repo_root).expanduser().resolve()
    model_dir = repo_root / MODEL_REL

    if not repo_root.exists():
        print(f"Repo root does not exist: {repo_root}", file=sys.stderr)
        return 1

    if not model_dir.exists():
        print(f"Model directory not found: {model_dir}", file=sys.stderr)
        return 2

    timestamp = _dt.datetime.now().strftime("%Y%m%d_%H%M%S")
    backup_root = repo_root / f"_phase1_region_model_backup_{timestamp}"
    legacy_anchor_dir = backup_root / "_legacy_anchor_model"

    targets = [
        model_dir / "ShapeRegionX.cs",
        model_dir / "ShapeRegionY.cs",
        model_dir / "CanvasPoint.cs",
        model_dir / "ProfilePoint.cs",
        model_dir / "ProfileXRegion.cs",
        model_dir / "ProfileZRegion.cs",
    ]

    if args.archive_anchors:
        targets.extend(
            [
                model_dir / "CanvasAnchorX.cs",
                model_dir / "CanvasAnchorY.cs",
            ]
        )

    print(f"Repo root : {repo_root}")
    print(f"Model dir : {model_dir}")
    print(f"Backup dir: {backup_root}")
    print()

    for path in targets:
        backup_file(path, backup_root, repo_root, args.dry_run)

    shape_region_x = '''using System;

namespace DLN
{
    [Serializable]
    public enum ShapeRegionX
    {
        Negative = 0,
        Middle = 1,
        Positive = 2,
    }
}
'''

    shape_region_y = '''using System;

namespace DLN
{
    [Serializable]
    public enum ShapeRegionY
    {
        Negative = 0,
        Middle = 1,
        Positive = 2,
    }
}
'''

    canvas_point = '''using System;
using UnityEngine;

namespace DLN
{
    /// <summary>
    /// Phase 1 region-based shape point.
    /// Stores a 3x3 region selection plus interpolation within that region.
    /// regionLerp is expected to stay in 0..1 on each axis, but is not clamped here.
    /// </summary>
    [Serializable]
    public struct CanvasPoint
    {
        public ShapeRegionX xRegion;
        public ShapeRegionY yRegion;
        public Vector2 regionLerp;

        public CanvasPoint(ShapeRegionX xRegion, ShapeRegionY yRegion, Vector2 regionLerp)
        {
            this.xRegion = xRegion;
            this.yRegion = yRegion;
            this.regionLerp = regionLerp;
        }

        public static CanvasPoint Center =>
            new CanvasPoint(
                ShapeRegionX.Middle,
                ShapeRegionY.Middle,
                new Vector2(0.5f, 0.5f)
            );

        public override readonly string ToString()
        {
            return $"CanvasPoint({xRegion}, {yRegion}, {regionLerp})";
        }
    }
}
'''

    profile_x_region = '''using System;

namespace DLN
{
    /// <summary>
    /// X runs from padding edge to border edge, with content edge as the divider.
    /// Inner = padding-to-content
    /// Outer = content-to-border
    /// </summary>
    [Serializable]
    public enum ProfileXRegion
    {
        Inner = 0,
        Outer = 1,
    }
}
'''

    profile_z_region = '''using System;

namespace DLN
{
    /// <summary>
    /// Vertical profile bands from top to bottom:
    /// 1) +border to +content
    /// 2) +content to +padding
    /// 3) +padding to -padding
    /// 4) -padding to -content
    /// 5) -content to -border
    /// </summary>
    [Serializable]
    public enum ProfileZRegion
    {
        PositiveOuter = 0,
        PositiveInner = 1,
        Center = 2,
        NegativeInner = 3,
        NegativeOuter = 4,
    }
}
'''

    profile_point = '''using System;
using UnityEngine;

namespace DLN
{
    /// <summary>
    /// Phase 1 region-based profile point.
    /// Stores a 2x5 region selection plus interpolation within that region.
    /// regionLerp.x is local interpolation within the X region.
    /// regionLerp.y is local interpolation within the Z region.
    /// </summary>
    [Serializable]
    public struct ProfilePoint
    {
        public ProfileXRegion xRegion;
        public ProfileZRegion zRegion;
        public Vector2 regionLerp;

        public ProfilePoint(ProfileXRegion xRegion, ProfileZRegion zRegion, Vector2 regionLerp)
        {
            this.xRegion = xRegion;
            this.zRegion = zRegion;
            this.regionLerp = regionLerp;
        }

        public static ProfilePoint Center =>
            new ProfilePoint(
                ProfileXRegion.Inner,
                ProfileZRegion.Center,
                new Vector2(0.5f, 0.5f)
            );

        public override readonly string ToString()
        {
            return $"ProfilePoint({xRegion}, {zRegion}, {regionLerp})";
        }
    }
}
'''

    outputs = {
        model_dir / "ShapeRegionX.cs": shape_region_x,
        model_dir / "ShapeRegionY.cs": shape_region_y,
        model_dir / "CanvasPoint.cs": canvas_point,
        model_dir / "ProfileXRegion.cs": profile_x_region,
        model_dir / "ProfileZRegion.cs": profile_z_region,
        model_dir / "ProfilePoint.cs": profile_point,
    }

    for path, text in outputs.items():
        write_text(path, text, args.dry_run)

    if args.archive_anchors:
        for old_name in ("CanvasAnchorX.cs", "CanvasAnchorY.cs"):
            src = model_dir / old_name
            archive_file(src, legacy_anchor_dir, args.dry_run)
    else:
        print("[note] Legacy anchor files left in place for now.")
        print("       Use --archive-anchors once resolver/policy code is migrated.")

    print()
    print("Phase 1 model migration complete.")
    print("Next recommended steps:")
    print("  1. Update ShapeCanvasPointResolver.cs to map canvas position <-> ShapeRegion + lerp")
    print("  2. Update ProfileCanvasPointResolver.cs to map canvas position <-> ProfileX/ZRegion + lerp")
    print("  3. Move slice boundary math into ProfileRegionLayout.cs and shape layout helpers")
    print("  4. Remove anchor references from policies/drawing after round-trip parity")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
