#!/usr/bin/env python3
from __future__ import annotations

import json
import os
import subprocess
import sys
import tempfile
from pathlib import Path


def repository_root() -> Path:
    return Path(__file__).resolve().parent.parent


def is_under(path: Path, root: Path) -> bool:
    resolved = path.resolve()
    root_resolved = root.resolve()
    return resolved == root_resolved or root_resolved in resolved.parents


def run(root: Path, args: list[str], env: dict[str, str]) -> None:
    result = subprocess.run(
        args,
        cwd=root,
        env=env,
        text=True,
        capture_output=True,
    )
    if result.returncode != 0:
        detail = result.stderr.strip() or result.stdout.strip() or "no output"
        raise SystemExit(f"{' '.join(args)} failed ({result.returncode}): {detail}")


def restore_webapp_with_environment(root: Path, env: dict[str, str]) -> None:
    run(
        root,
        ["dotnet", "restore", "src/WebApp/WebApp.csproj", "--force-evaluate", "-v", "quiet"],
        env,
    )


def fail(message: str) -> None:
    raise SystemExit(message)


def package_folders(assets: dict) -> list[Path]:
    folders = assets.get("packageFolders") or {}
    if not folders:
        fail("project.assets.json has no packageFolders")
    return [Path(path) for path in folders]


def framework_asset(manifest: dict) -> dict:
    node = manifest.get("Root") or {}
    for part in ("_framework", "blazor.web.js"):
        children = node.get("Children") or {}
        if part not in children:
            fail("runtime manifest missing _framework/blazor.web.js")
        node = children[part]
    asset = node.get("Asset")
    if not isinstance(asset, dict):
        fail("runtime manifest missing Asset for _framework/blazor.web.js")
    return asset


def verify_assets(assets_path: Path, stable: Path, probe_root: Path) -> list[Path]:
    if not assets_path.is_file():
        fail(f"missing {assets_path}")
    assets = json.loads(assets_path.read_text())
    folders = package_folders(assets)
    for folder in folders:
        if is_under(folder, probe_root):
            fail(f"packageFolders still under probe temp: {folders}")
        if folder.resolve() != stable:
            fail(f"packageFolders { [str(path) for path in folders] } != {stable}")
    return folders


def verify_runtime(runtime_path: Path, probe_root: Path) -> Path:
    if not runtime_path.is_file():
        fail(f"missing {runtime_path}")
    manifest = json.loads(runtime_path.read_text())
    roots = manifest.get("ContentRoots") or []
    temp_roots = [root for root in roots if is_under(Path(root), probe_root)]
    if temp_roots:
        fail(f"ContentRoots under probe temp: {temp_roots}")
    asset = framework_asset(manifest)
    try:
        index = int(asset["ContentRootIndex"])
    except (KeyError, TypeError, ValueError):
        fail(f"invalid ContentRootIndex on _framework/blazor.web.js: {asset!r}")
    sub_path = asset.get("SubPath")
    if not sub_path:
        fail("missing SubPath on _framework/blazor.web.js")
    if index < 0 or index >= len(roots):
        fail(f"ContentRootIndex {index} out of range ({len(roots)} roots)")
    resolved = Path(roots[index]) / sub_path
    if not resolved.is_file():
        fail(f"blazor.web.js does not exist: {resolved}")
    return resolved


def main() -> int:
    root = repository_root()
    project = root / "src/WebApp/WebApp.csproj"
    if not project.is_file():
        fail(f"WebApp project not found under {root}")
    stable = (Path.home() / ".nuget" / "packages").resolve()
    assets_path = root / "artifacts/obj/WebApp/project.assets.json"
    runtime_path = root / "artifacts/bin/WebApp/debug/WebApp.staticwebassets.runtime.json"
    original_env = os.environ.copy()
    try:
        with tempfile.TemporaryDirectory(prefix="eshop-nuget-verify-") as tmp:
            probe_root = Path(tmp)
            probe = probe_root / "packages"
            probe.mkdir()
            env = original_env.copy()
            env["NUGET_PACKAGES"] = str(probe)
            env.pop("RestorePackagesPath", None)
            restore_webapp_with_environment(root, env)
            folders = verify_assets(assets_path, stable, probe_root)
            run(
                root,
                ["dotnet", "build", "src/WebApp/WebApp.csproj", "--no-restore", "-v", "quiet"],
                env,
            )
            blazor = verify_runtime(runtime_path, probe_root)
            print(f"probe={probe}")
            print(f"packageFolders={','.join(str(path.resolve()) for path in folders)}")
            print(f"stable={stable}")
            print(f"blazor={blazor}")
            return 0
    finally:
        try:
            restore_webapp_with_environment(root, original_env)
        except SystemExit as cleanup:
            print(f"cleanup restore failed: {cleanup}", file=sys.stderr)


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except subprocess.CalledProcessError as error:
        raise SystemExit(f"verify failed: {error}") from error
