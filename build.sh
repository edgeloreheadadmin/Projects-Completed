#!/usr/bin/env bash
# build.sh — Compile UnityStubs and AurasLib to Bridge/bin/.
set -euo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BIN="$REPO/Bridge/bin"
mkdir -p "$BIN"

echo "=== Building UnityStubs ==="
dotnet build "$REPO/Bridge/UnityStubs/UnityStubs.csproj" \
    -c Release -o "$BIN" --nologo

echo ""
echo "=== Building AurasLib ==="
dotnet build "$REPO/Bridge/AurasLib/AurasLib.csproj" \
    -c Release -o "$BIN" --nologo

echo ""
echo "Build complete. DLLs in $BIN/"
ls -lh "$BIN"/*.dll
