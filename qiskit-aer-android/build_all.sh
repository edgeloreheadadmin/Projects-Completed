#!/usr/bin/env bash
# One-shot build script: downloads NDK + Aer, builds OpenBLAS (optional),
# and cross-compiles libaer_simulator.so for Android arm64-v8a.
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")" && pwd)"
SCRIPTS="$ROOT_DIR/scripts"

echo "================================================================"
echo "  Qiskit Aer Android Build"
echo "  Target: arm64-v8a, API 24+"
echo "================================================================"

# 1 – Android NDK r26d
bash "$SCRIPTS/01_download_ndk.sh"

# 2 – Qiskit Aer 0.14.2 source
bash "$SCRIPTS/02_download_aer.sh"

# 3 – OpenBLAS for arm64-v8a (optional but improves dense-matrix performance)
#     Comment out if you want a faster build without BLAS.
bash "$SCRIPTS/03_build_openblas.sh"

# 4 – Build the native shared library
bash "$SCRIPTS/04_build_aer_android.sh"

echo ""
echo "================================================================"
echo "  Build complete!"
echo ""
echo "  Output artifacts:"
ls -lh "$ROOT_DIR/dist/lib/arm64-v8a/" 2>/dev/null || true
echo ""
echo "  Copy to your Android project:"
echo "    cp dist/lib/arm64-v8a/libaer_simulator.so \\"
echo "       <AndroidProject>/app/src/main/jniLibs/arm64-v8a/"
echo "    cp dist/java/com/qiskit/aer/AerSimulator.java \\"
echo "       <AndroidProject>/app/src/main/java/com/qiskit/aer/"
echo ""
echo "  If you use c++_shared, also copy:"
echo "    \$NDK/toolchains/llvm/prebuilt/linux-x86_64/sysroot/usr/lib/"
echo "    aarch64-linux-android/libc++_shared.so"
echo "================================================================"
