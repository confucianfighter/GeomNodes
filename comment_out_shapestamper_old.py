#!/usr/bin/env python3
"""
Comment out all C# files under Layout/Editor/ShapeStamperWindowOld by wrapping each file in:

#if false
...original contents...
#endif

Usage:
    python comment_out_shapestamper_old.py /path/to/GeomNodes
    python comment_out_shapestamper_old.py /path/to/GeomNodes --dry-run
    python comment_out_shapestamper_old.py /path/to/GeomNodes --undo
"""

from __future__ import annotations

import argparse
import datetime as _dt
import shutil
import sys
from pathlib import Path

TARGET_REL = Path("Assets/Nodes/Layout/Editor/ShapeStamperWindowOld")
BACKUP_PREFIX = "_shapestamper_old_comment_backup_"


def is_already_wrapped(text: str) -> bool:
    stripped = text.strip()
    return stripped.startswith("#if false") and stripped.endswith("#endif")


def wrap_text(text: str) -> str:
    text = text.replace("\r\n", "\n").replace("\r", "\n")
    if text.endswith("\n"):
        return f"#if false\n{text}#endif\n"
    return f"#if false\n{text}\n#endif\n"


def backup_file(src: Path, backup_root: Path, repo_root: Path, dry_run: bool) -> None:
    rel = src.relative_to(repo_root)
    dst = backup_root / rel
    if dry_run:
        print(f"[dry-run] backup {src} -> {dst}")
        return
    dst.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(src, dst)
    print(f"[backup] {src} -> {dst}")


def find_latest_backup(repo_root: Path):
    candidates = sorted(
        [p for p in repo_root.iterdir() if p.is_dir() and p.name.startswith(BACKUP_PREFIX)],
        reverse=True,
    )
    return candidates[0] if candidates else None


def undo_latest_backup(repo_root: Path, dry_run: bool) -> int:
    backup_root = find_latest_backup(repo_root)
    if backup_root is None:
        print("No backup folder found to restore.", file=sys.stderr)
        return 2

    restored = 0
    for backup_file_path in backup_root.rglob("*.cs"):
        rel = backup_file_path.relative_to(backup_root)
        dst = repo_root / rel
        if dry_run:
            print(f"[dry-run] restore {backup_file_path} -> {dst}")
        else:
            dst.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(backup_file_path, dst)
            print(f"[restore] {backup_file_path} -> {dst}")
        restored += 1

    print(f"\nRestored {restored} file(s) from {backup_root}")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("repo_root", help="Path to the Unity repo root")
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--undo", action="store_true", help="Restore from the most recent backup")
    args = parser.parse_args()

    repo_root = Path(args.repo_root).expanduser().resolve()
    target_dir = repo_root / TARGET_REL

    if not repo_root.exists():
        print(f"Repo root does not exist: {repo_root}", file=sys.stderr)
        return 1

    if args.undo:
        return undo_latest_backup(repo_root, args.dry_run)

    if not target_dir.exists():
        print(f"Target directory not found: {target_dir}", file=sys.stderr)
        return 1

    cs_files = sorted(target_dir.rglob("*.cs"))
    if not cs_files:
        print(f"No .cs files found under: {target_dir}")
        return 0

    timestamp = _dt.datetime.now().strftime("%Y%m%d_%H%M%S")
    backup_root = repo_root / f"{BACKUP_PREFIX}{timestamp}"

    print(f"Repo root : {repo_root}")
    print(f"Target dir: {target_dir}")
    print(f"Backup dir: {backup_root}")
    print()

    changed = 0
    skipped = 0

    for path in cs_files:
        original = path.read_text(encoding="utf-8")
        if is_already_wrapped(original):
            print(f"[skip] already wrapped: {path}")
            skipped += 1
            continue

        backup_file(path, backup_root, repo_root, args.dry_run)
        wrapped = wrap_text(original)

        if args.dry_run:
            print(f"[dry-run] wrap {path}")
        else:
            path.write_text(wrapped, encoding="utf-8", newline="\n")
            print(f"[write] {path}")
        changed += 1

    print()
    print(f"Done. Wrapped {changed} file(s), skipped {skipped} already-wrapped file(s).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
