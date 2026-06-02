#!/usr/bin/env bash
# Builds libneat.so for Android arm64-v8a using the NDK from the sibling
# qiskit-aer-android project (or any NDK r26+ at the path below).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
NDK_DIR="$ROOT_DIR/../qiskit-aer-android/toolchain/android-ndk-r26d"
BUILD_DIR="$ROOT_DIR/build/arm64-v8a"
INSTALL_DIR="$ROOT_DIR/dist"

ANDROID_API=24
ABI="arm64-v8a"

if [ ! -d "$NDK_DIR" ]; then
    echo "[Build] ERROR: NDK not found at $NDK_DIR"
    echo "        Run qiskit-aer-android/scripts/01_download_ndk.sh first,"
    echo "        or set NDK_DIR to your NDK path."
    exit 1
fi

mkdir -p "$BUILD_DIR" "$INSTALL_DIR"

echo "[NEAT] Configuring for $ABI, API $ANDROID_API ..."
cmake \
    -S "$ROOT_DIR/jni" \
    -B "$BUILD_DIR" \
    -G Ninja \
    -DCMAKE_MAKE_PROGRAM="$(which ninja)" \
    -DCMAKE_TOOLCHAIN_FILE="$NDK_DIR/build/cmake/android.toolchain.cmake" \
    -DANDROID_ABI="$ABI" \
    -DANDROID_PLATFORM="android-${ANDROID_API}" \
    -DANDROID_STL="c++_shared" \
    -DCMAKE_BUILD_TYPE=Release \
    -DCMAKE_INSTALL_PREFIX="$INSTALL_DIR"

echo "[NEAT] Compiling ..."
cmake --build "$BUILD_DIR" --parallel "$(nproc)"

echo "[NEAT] Installing ..."
cmake --install "$BUILD_DIR"

# Strip debug symbols
STRIP="$NDK_DIR/toolchains/llvm/prebuilt/linux-x86_64/bin/llvm-strip"
SO="$INSTALL_DIR/lib/$ABI/libneat.so"
cp "$SO" "${SO%.so}.debug.so"
"$STRIP" --strip-unneeded "$SO"

# Bundle libc++_shared.so (required runtime dep)
LIBCXX=$(find "$NDK_DIR" -name "libc++_shared.so" -path "*aarch64*" | head -1)
[ -n "$LIBCXX" ] && cp "$LIBCXX" "$INSTALL_DIR/lib/$ABI/"

echo ""
echo "[NEAT] Done!"
ls -lh "$INSTALL_DIR/lib/$ABI/"
