#!/usr/bin/env bash
# Cross-compiles OpenBLAS for Android arm64-v8a using the NDK
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
NDK_DIR="$ROOT_DIR/toolchain/android-ndk-r26d"
OPENBLAS_VERSION="0.3.26"
OPENBLAS_DIR="$ROOT_DIR/openblas-src"
INSTALL_DIR="$ROOT_DIR/sysroot/arm64-v8a"
OPENBLAS_URL="https://github.com/OpenMathLib/OpenBLAS/releases/download/v${OPENBLAS_VERSION}/OpenBLAS-${OPENBLAS_VERSION}.tar.gz"

ANDROID_API=24
TRIPLE="aarch64-linux-android"
TOOLCHAIN="$NDK_DIR/toolchains/llvm/prebuilt/linux-x86_64"
CC="$TOOLCHAIN/bin/${TRIPLE}${ANDROID_API}-clang"
CXX="$TOOLCHAIN/bin/${TRIPLE}${ANDROID_API}-clang++"
AR="$TOOLCHAIN/bin/llvm-ar"
RANLIB="$TOOLCHAIN/bin/llvm-ranlib"

mkdir -p "$INSTALL_DIR"

if [ -f "$INSTALL_DIR/lib/libopenblas.a" ]; then
    echo "[OpenBLAS] Already built"
    exit 0
fi

if [ ! -d "$OPENBLAS_DIR" ]; then
    echo "[OpenBLAS] Downloading ${OPENBLAS_VERSION}..."
    TMP="$(mktemp -d)"
    curl -L --retry 4 --retry-delay 5 -o "$TMP/openblas.tar.gz" "$OPENBLAS_URL"
    tar -xzf "$TMP/openblas.tar.gz" -C "$TMP"
    mv "$TMP/OpenBLAS-${OPENBLAS_VERSION}" "$OPENBLAS_DIR"
    rm -rf "$TMP"
fi

echo "[OpenBLAS] Building for arm64-v8a..."
make -C "$OPENBLAS_DIR" \
    CC="$CC" \
    CXX="$CXX" \
    AR="$AR" \
    RANLIB="$RANLIB" \
    TARGET=ARMV8 \
    HOSTCC=gcc \
    NOFORTRAN=1 \
    NO_SHARED=0 \
    NO_STATIC=0 \
    BINARY=64 \
    CROSS=1 \
    CROSS_SUFFIX="${TOOLCHAIN}/bin/llvm-" \
    PREFIX="$INSTALL_DIR" \
    NUM_THREADS=4 \
    -j4

make -C "$OPENBLAS_DIR" \
    PREFIX="$INSTALL_DIR" \
    NO_SHARED=0 \
    install

echo "[OpenBLAS] Done -> $INSTALL_DIR"
