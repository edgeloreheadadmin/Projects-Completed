#!/usr/bin/env bash
# setup.sh — Install .NET SDK 8 and Python dependencies.
set -euo pipefail

echo "=== Installing dotnet-sdk-8.0 ==="
sudo apt-get update -qq
sudo apt-get install -y dotnet-sdk-8.0

echo ""
echo "=== Installing Python packages ==="
pip3 install "pythonnet>=3.0.3" "pygame>=2.5.0"

echo ""
echo "Setup complete. Now run: bash build.sh"
