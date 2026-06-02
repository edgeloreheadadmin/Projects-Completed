#!/usr/bin/env bash
# Downloads Qiskit Aer source (tag 0.14.2)
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
AER_VERSION="0.14.2"
AER_DIR="$ROOT_DIR/qiskit-aer-src"
AER_URL="https://github.com/Qiskit/qiskit-aer/archive/refs/tags/${AER_VERSION}.tar.gz"

if [ -d "$AER_DIR" ]; then
    echo "[Aer] Source already present at $AER_DIR"
    exit 0
fi

echo "[Aer] Downloading Qiskit Aer ${AER_VERSION}..."
TMP="$(mktemp -d)"
curl -L --retry 4 --retry-delay 5 -o "$TMP/aer.tar.gz" "$AER_URL"
echo "[Aer] Extracting..."
tar -xzf "$TMP/aer.tar.gz" -C "$TMP"
mv "$TMP/qiskit-aer-${AER_VERSION}" "$AER_DIR"
rm -rf "$TMP"
echo "[Aer] Done -> $AER_DIR"
