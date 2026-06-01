"""
aura_engine.py — Pythonic wrapper around the C# AuraSimulator.

Handles Python↔C# type coercion so callers work with plain Python floats,
ints, strings, and dicts instead of CLR types.
"""
import bridge  # ensures CLR is loaded before any Auras import

from Auras import AuraSimulator  # C# class via Python.NET

LAYER_NAMES = [
    "Etheric",
    "Emotional",
    "Mental",
    "Astral",
    "EthericTemplate",
    "Celestial",
    "Causal",
]


class AuraEngine:
    """Python facade over C# AuraSimulator."""

    def __init__(self):
        self._sim = AuraSimulator()

    # ── Simulation control ─────────────────────────────────────────────────

    def update(self, delta_time: float) -> None:
        """Advance simulation by delta_time seconds (recommended: cap at 0.1)."""
        self._sim.Update(float(delta_time))

    def reset(self) -> None:
        """Re-initialize to neutral aura state."""
        self._sim.Initialize()

    # ── Layer inputs ───────────────────────────────────────────────────────

    def set_layer_input(self, layer_index: int, value: float) -> None:
        """Set the direct input for a layer (0=Etheric … 6=Causal). Value clamped to [0, 1]."""
        self._sim.SetLayerInput(int(layer_index), float(value))

    def get_layer_input(self, layer_index: int) -> float:
        """Get the current direct input for a layer."""
        return float(self._sim.GetLayerInput(int(layer_index)))

    # ── Environment / mode settings ───────────────────────────────────────

    def set_environment_modes(
        self,
        circadian: int = 2,   # 0=Disrupted 1=Recovering 2=Aligned 3=Coherent
        social: int = 1,      # 0=Protected 1=Supportive 2=Charged 3=Overwhelming
        information: int = 1, # 0=Quiet 1=Focused 2=Saturated 3=Noisy
        practice: int = 2,    # 0=Grounding 1=EmotionalRelease 2=HeartCoherence 3=Meditation 4=Service
    ) -> None:
        self._sim.SetEnvironmentModes(int(circadian), int(social), int(information), int(practice))

    def set_support_inputs(
        self,
        geomagnetic: float = 0.3,
        nature: float = 0.7,
        boundaries: float = 0.72,
        heart: float = 0.76,
        contemplative: float = 0.72,
    ) -> None:
        self._sim.SetSupportInputs(
            float(geomagnetic), float(nature), float(boundaries),
            float(heart), float(contemplative),
        )

    # ── Rendering parameters ───────────────────────────────────────────────

    def set_blend_speed(self, speed: float) -> None:
        self._sim.SetBlendSpeed(float(speed))

    def set_pulse_amplitude(self, amplitude: float) -> None:
        self._sim.SetPulseAmplitude(float(amplitude))

    # ── Read-back ──────────────────────────────────────────────────────────

    def get_snapshot(self, layer_index: int) -> dict:
        """Return a dict with the current state of one aura layer."""
        snap = self._sim.GetLayerSnapshot(int(layer_index))
        return _snap_to_dict(snap)

    def get_all_snapshots(self) -> list:
        """Return a list of 7 dicts, one per aura layer (Etheric=0 … Causal=6)."""
        raw = self._sim.GetAllSnapshots()
        return [_snap_to_dict(s) for s in raw]


def _snap_to_dict(snap) -> dict:
    """Convert a C# AuraLayerSnapshot to a plain Python dict."""
    return {
        "layer_id":   int(snap.LayerId),
        "layer_name": str(snap.LayerName),
        "intensity":  float(snap.Intensity),
        "integrity":  float(snap.Integrity),
        "energy":     float(snap.Energy),
        "color":      (float(snap.ColorR), float(snap.ColorG), float(snap.ColorB)),
        "scale":      float(snap.Scale),
        "pulse_phase": float(snap.PulsePhase),
    }
