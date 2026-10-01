#!/usr/bin/env bash
#
# Build the UMW shader asset bundle(s) with the Windows Unity editor, driven from WSL.
#
#   ./build.sh [win|linux|mac ...]      default: win
#
# The project source lives here (Assets/, Packages/manifest.json, ProjectSettings/ProjectVersion.txt)
# but the build runs in a mirror on the Windows filesystem (%LOCALAPPDATA%\umw-unity): Unity's
# Library/ is thousands of small files, and driving that over the WSL 9P share is far too slow.
# rsync keeps the mirror's Assets/ exact (--delete), Unity regenerates everything else, and the
# finished bundle is copied back into
# 1.6/Mods/VanillaFactionsExpandedPirates/AssetBundles/ (the only shipped artefact). Unity's
# per-directory master bundle and the .manifest files are deliberately left behind: RimWorld's
# ModAssetBundlesHandler loads EVERY extensionless file under AssetBundles/, so a stray master
# bundle would be loaded too.
#
# Requirements: Unity 2022.3.35f1 (RimWorld 1.6's exact version) installed through Unity Hub with
# the build-support module for each requested OS (Windows is built into the Windows editor), and a
# licence activated on this machine (Unity Hub sign-in). Log: %LOCALAPPDATA%\umw-unity\build-<os>.log.
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO="$(cd "$HERE/../.." && pwd)"
BUNDLE_DIR="$REPO/1.6/Mods/VanillaFactionsExpandedPirates/AssetBundles"
UNITY="${UNITY_EXE:-/mnt/c/Program Files/Unity/Hub/Editor/2022.3.35f1/Editor/Unity.exe}"
[ -x "$UNITY" ] || { echo "Unity editor not found at: $UNITY (set UNITY_EXE)"; exit 1; }

LOCALAPPDATA_WIN="$(cmd.exe /c 'echo %LOCALAPPDATA%' 2>/dev/null | tr -d '\r')"
MIRROR="${UMW_UNITY_MIRROR:-$(wslpath -u "$LOCALAPPDATA_WIN")/umw-unity}"
MIRROR_WIN="$(wslpath -w "$MIRROR")"
mkdir -p "$MIRROR"

rsync -a --delete --exclude '*.meta' "$HERE/Assets/" "$MIRROR/Assets/"
rsync -a "$HERE/Packages/" "$MIRROR/Packages/"
rsync -a "$HERE/ProjectSettings/" "$MIRROR/ProjectSettings/"

targets=("$@"); [ ${#targets[@]} -gt 0 ] || targets=(win)
for os in "${targets[@]}"; do
  case "$os" in
    win)   method=UMW.BuildAssetBundles.BuildWindows; bt=Win64 ;;
    linux) method=UMW.BuildAssetBundles.BuildLinux;   bt=Linux64 ;;
    mac)   method=UMW.BuildAssetBundles.BuildMac;     bt=OSXUniversal ;;
    *) echo "unknown target: $os"; exit 1 ;;
  esac
  log="$MIRROR_WIN\\build-$os.log"
  echo "== building $os (log: $log)"
  rm -f "$MIRROR/Output/$os/umw_shaders_$os"
  set +e
  "$UNITY" -batchmode -nographics -quit -projectPath "$MIRROR_WIN" -buildTarget "$bt" \
    -executeMethod "$method" -logFile "$log"
  rc=$?
  set -e
  grep -a '\[UMW\]\|Shader error\|Shader warning\|No valid Unity Editor license\|Aborting batchmode' "$MIRROR/build-$os.log" || true
  if [ $rc -ne 0 ] || [ ! -f "$MIRROR/Output/$os/umw_shaders_$os" ]; then
    echo "!! Unity exited with $rc and no bundle; see $log"; exit 1
  fi
  mkdir -p "$BUNDLE_DIR"
  cp "$MIRROR/Output/$os/umw_shaders_$os" "$BUNDLE_DIR/umw_shaders_$os"
  echo "== wrote 1.6/Mods/VanillaFactionsExpandedPirates/AssetBundles/umw_shaders_$os ($(stat -c %s "$BUNDLE_DIR/umw_shaders_$os") bytes)"
done
