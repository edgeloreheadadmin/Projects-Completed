"""
bridge.py — CLR initialization and DLL loading for the Auras C#/Python bridge.

Must be imported before any Auras or UnityEngine imports. Uses Python.NET 3.x
with the coreclr backend (requires dotnet-sdk-8.0 installed via apt).
"""
import os
import sys

# Python.NET 3.x: load() must be called before `import clr`.
import pythonnet
pythonnet.load("coreclr")

import clr  # noqa: E402  (must follow pythonnet.load)

_HERE = os.path.dirname(os.path.abspath(__file__))
_BIN  = os.path.abspath(os.path.join(_HERE, "..", "Bridge", "bin"))

if not os.path.isdir(_BIN):
    raise RuntimeError(
        f"Bridge/bin directory not found at {_BIN}\n"
        "Run `bash build.sh` from the repo root first."
    )

_UNITY_STUBS = os.path.join(_BIN, "UnityStubs.dll")
_AURAS_LIB   = os.path.join(_BIN, "AurasLib.dll")

for dll in (_UNITY_STUBS, _AURAS_LIB):
    if not os.path.isfile(dll):
        raise RuntimeError(
            f"DLL not found: {dll}\n"
            "Run `bash build.sh` from the repo root first."
        )

# Add the bin directory to the assembly search path so AurasLib can resolve UnityStubs.
sys.path.insert(0, _BIN)

clr.AddReference(_UNITY_STUBS)
clr.AddReference(_AURAS_LIB)

# Make namespaces importable as Python modules.
import UnityEngine            # noqa: F401, E402  — Unity stub types (Color, Mathf, etc.)
import Auras                  # noqa: F401, E402  — AuraSimulator, AuraLayerSnapshot
import Auras.Core             # noqa: F401, E402  — AuraLayerContext, EasingCurve, etc.
import Auras.BiofieldAuraMatrix  # noqa: F401, E402  — BiofieldLayerType enums, etc.

__all__ = ["UnityEngine", "Auras"]
