#!/usr/bin/env bash
# Cross-compiles Qiskit Aer for Android arm64-v8a using the NDK.
# Prerequisites: NDK at toolchain/android-ndk-r26d, Aer source at qiskit-aer-src/
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
NDK_DIR="$ROOT_DIR/toolchain/android-ndk-r26d"
AER_ROOT="$ROOT_DIR/qiskit-aer-src"
BUILD_DIR="$ROOT_DIR/build/arm64-v8a"
INSTALL_DIR="$ROOT_DIR/dist"
SYSROOT="$ROOT_DIR/sysroot/arm64-v8a"  # optional, for pre-built OpenBLAS

ANDROID_API=24
ABI="arm64-v8a"

if [ ! -d "$NDK_DIR" ]; then
    echo "[Build] ERROR: NDK not found. Run scripts/01_download_ndk.sh first."
    exit 1
fi
if [ ! -d "$AER_ROOT/src" ]; then
    echo "[Build] ERROR: Aer source not found. Run scripts/02_download_aer.sh first."
    exit 1
fi

mkdir -p "$BUILD_DIR" "$INSTALL_DIR"

SYSROOT_ARG=""
if [ -d "$SYSROOT" ] && [ -f "$SYSROOT/lib/libopenblas.so" ]; then
    SYSROOT_ARG="-DAER_SYSROOT=$SYSROOT"
    echo "[Build] OpenBLAS sysroot found, enabling BLAS acceleration."
fi

echo "[Build] Configuring CMake for $ABI, API $ANDROID_API ..."
cmake \
    -S "$ROOT_DIR/jni" \
    -B "$BUILD_DIR" \
    -G Ninja \
    -DCMAKE_TOOLCHAIN_FILE="$NDK_DIR/build/cmake/android.toolchain.cmake" \
    -DANDROID_ABI="$ABI" \
    -DANDROID_PLATFORM="android-${ANDROID_API}" \
    -DANDROID_STL="c++_shared" \
    -DCMAKE_BUILD_TYPE=Release \
    -DAER_ROOT="$AER_ROOT" \
    $SYSROOT_ARG \
    -DCMAKE_INSTALL_PREFIX="$INSTALL_DIR"

echo "[Build] Compiling (this takes several minutes) ..."
cmake --build "$BUILD_DIR" --parallel 4

echo "[Build] Installing ..."
cmake --install "$BUILD_DIR"

echo ""
echo "[Build] ✓ Done!"
echo "  libaer_simulator.so → $INSTALL_DIR/lib/$ABI/"
echo "  AerSimulator.java   → $INSTALL_DIR/java/com/qiskit/aer/"
echo "  aer_runtime_api.h   → $INSTALL_DIR/include/"
