#!/usr/bin/env python3
"""Create a fresh FE demo worktree without rewriting or deleting Git history."""
from __future__ import annotations

import argparse
import datetime as dt
import subprocess
import sys
from pathlib import Path


def git(root: Path, *args: str, capture: bool = False) -> str:
    result = subprocess.run(
        ["git", *args],
        cwd=root,
        check=True,
        text=True,
        capture_output=capture,
    )
    return result.stdout.strip() if capture else ""


def repository_root() -> Path:
    result = subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        check=True,
        text=True,
        capture_output=True,
    )
    return Path(result.stdout.strip()).resolve()


def worktree_paths(root: Path) -> set[Path]:
    output = git(root, "worktree", "list", "--porcelain", capture=True)
    return {
        Path(line.removeprefix("worktree ")).resolve()
        for line in output.splitlines()
        if line.startswith("worktree ")
    }


def changed_paths(worktree: Path) -> list[str]:
    output = git(worktree, "status", "--porcelain", capture=True)
    return [line[3:] for line in output.splitlines() if line]


def remove_previous(root: Path, target: Path) -> None:
    if target not in worktree_paths(root):
        if target.exists():
            raise RuntimeError(
                f"{target} exists but is not a registered Git worktree; move it manually"
            )
        return

    changes = changed_paths(target)
    if changes:
        formatted = "\n".join(f"  - {path}" for path in changes)
        raise RuntimeError(
            f"previous demo worktree has uncommitted changes:\n{formatted}\n"
            "Commit or preserve them, then run /reset-fe-demo again."
        )

    git(root, "worktree", "remove", str(target))


def branch_name(root: Path) -> str:
    stamp = dt.datetime.now(dt.UTC).strftime("%Y%m%d-%H%M%S")
    base = f"cursor/fe-demo-{stamp}"
    candidate = f"{base}-95f0"
    suffix = 2
    while subprocess.run(
        ["git", "show-ref", "--verify", "--quiet", f"refs/heads/{candidate}"],
        cwd=root,
    ).returncode == 0:
        candidate = f"{base}-{suffix}-95f0"
        suffix += 1
    return candidate


def create_fresh_worktree(root: Path, target: Path, base: str) -> str:
    git(root, "fetch", "origin", base)
    branch = branch_name(root)
    git(root, "worktree", "add", "-b", branch, str(target), f"origin/{base}")
    git(root, "branch", "--unset-upstream", branch)
    subprocess.run(["make", "demo-reset"], cwd=target, check=True)
    changes = changed_paths(target)
    if changes:
        formatted = "\n".join(f"  - {path}" for path in changes)
        raise RuntimeError(f"fresh demo worktree is unexpectedly dirty:\n{formatted}")
    return branch


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--base", default="main")
    parser.add_argument(
        "--target",
        type=Path,
        help="Dedicated demo worktree path (default: ~/.cursor/demo-worktrees/<repo>)",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    root = repository_root()
    target = (
        args.target
        or Path.home() / ".cursor" / "demo-worktrees" / root.name
    ).resolve()

    if root == target:
        raise RuntimeError("run /reset-fe-demo from the original clone, not the demo worktree")

    remove_previous(root, target)
    target.parent.mkdir(parents=True, exist_ok=True)
    branch = create_fresh_worktree(root, target, args.base)
    print(f"FE DEMO READY\npath: {target}\nbranch: {branch}")
    print("Open that path in a new Cursor window and start a new Agent chat.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (RuntimeError, subprocess.CalledProcessError) as error:
        print(f"FE DEMO RESET BLOCKED: {error}", file=sys.stderr)
        raise SystemExit(1)
