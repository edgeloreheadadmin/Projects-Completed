#!/usr/bin/env bash
# Downloads Android NDK r26d and extracts it to $NDK_DIR
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
NDK_VERSION="r26d"
NDK_DIR="$ROOT_DIR/toolchain/android-ndk-${NDK_VERSION}"
NDK_ZIP="$ROOT_DIR/toolchain/android-ndk-${NDK_VERSION}-linux.zip"
NDK_URL="https://dl.google.com/android/repository/android-ndk-${NDK_VERSION}-linux.zip"

mkdir -p "$ROOT_DIR/toolchain"

if [ -d "$NDK_DIR" ]; then
    echo "[NDK] Already present at $NDK_DIR"
    exit 0
fi

echo "[NDK] Downloading Android NDK ${NDK_VERSION}..."
curl -L --retry 4 --retry-delay 5 -o "$NDK_ZIP" "$NDK_URL"
echo "[NDK] Extracting..."
unzip -q "$NDK_ZIP" -d "$ROOT_DIR/toolchain"
rm -f "$NDK_ZIP"
echo "[NDK] Done -> $NDK_DIR"
