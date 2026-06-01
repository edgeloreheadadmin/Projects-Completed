# Phase 3: Explicit Cascade/Dependency Model

## Overview
Phase 3 replaces implicit hardcoded weights with an explicit, editable dependency graph. Layers are computed in topology-ordered sequence, where each layer's state is determined by its incoming dependencies rather than pre-calculated constants.

## Architecture

### Core Components

#### 1. **DependencyGraphComputer.cs**
Main computation engine:
- `BuildComputationOrder()`: Topology-ordered traversal (DFS)
- `ComputeLayerStates()`: Cascade computation following dependency graph
- `GetSourceMetricValue()`: Extract intensity/integrity/energy from states
- `ApplyInfluenceCurve()`: Nonlinear influence functions
- Runtime dependency modification (Add/Remove at runtime)
- Graph visualization and debugging

#### 2. **BiofieldAuraDriverV2.cs**
Enhanced driver with dual-mode operation:
- **New Mode (useDependencyGraph=true)**: Uses explicit dependency graph
- **Legacy Mode (useDependencyGraph=false)**: Original weighted calculations
- Seamless switching between modes
- Smooth fallback for compatibility
- InteractionProfile integration

#### 3. **DependencyGraphDebugger.cs**
Visualization and monitoring tools:
- Real-time graph visualization
- Frame-by-frame state monitoring
- Performance metrics tracking
- State snapshots and exports
- Layer boost testing

### Dependency Graph Structure

```
LayerDependency
├── sourceLayerId: which layer influences
├── targetLayerId: which layer is affected
├── strength: [0-1] influence magnitude
├── sourceMetric: "intensity" | "integrity" | "energy"
├── influenceCurve: easing curve for nonlinear influence
└── [configured in InteractionProfile]

InteractionProfile
├── LayerDependencies: list of all edges
├── EnvironmentWeights: mode-based scaling
├── SupportWeights: support input scaling
└── LayerWeights: layer-specific defaults
```

## Usage

### Setup

1. **Create InteractionProfile asset:**
   ```
   Right-click in Project → Create → Auras/Biofield Aura Matrix/Interaction Profile
   ```

2. **Add to BiofieldAuraDriverV2:**
   - In Inspector, set "Interaction Profile" field
   - Toggle "Use Dependency Graph" = true
   - Adjust "Environment Modifier" [0-1]

3. **Configure Dependencies:**
   - In InteractionProfile inspector
   - Add dependencies for layer interactions
   - Set strength and influence curves

### Example: Layer Dependency

**Setup:** Emotional layer influences Mental layer
```csharp
var dependency = new LayerDependency(
    sourceLayerId: (int)BiofieldLayerType.Emotional,
    targetLayerId: (int)BiofieldLayerType.Mental,
    strength: 0.3f,
    sourceMetric: "integrity"  // Use emotional integrity to influence mental
);

interactionProfile.AddDependency(dependency);
```

**Result:** Each frame, mental layer will receive:
```
mentalIntensity += emotionalIntegrity × 0.3 × strength
mentalIntegrity += emotionalIntegrity × 0.3 × strength
```

### Computation Sequence

Phase 3 computes layers in **topology order**:

1. **Layers with no dependencies** (independent) computed first
2. **Dependent layers** computed after their sources
3. **Cascade effects** naturally propagate through dependencies
4. **Circular dependencies** detected and warned

**Example Order for Research Defaults:**
```
1. Etheric (independent)
2. Emotional (independent)  
3. Mental (may depend on others)
4. Astral (may depend on emotional/mental)
5. EthericTemplate (template layer)
6. Celestial (high-order integration)
7. Causal (outermost, depends on many)
```

## Replacing Legacy Weights

### Before (Hardcoded Weights):
```csharp
BiofieldLayerState mental = new BiofieldLayerState(
    cognitiveOrder * 0.42f + 
    informationClarity * 0.2f + 
    circadianSupport * 0.12f + 
    contemplativeDepth * 0.1f + 
    clarityField * 0.08f + 
    earthPulse * 0.04f - 
    informationNoise * 0.18f - 
    circadianDisruption * 0.08f,
    // ... 80 lines of similar calculations
);
```

### After (Explicit Dependencies):
```
Mental Layer Dependencies:
  ← CognitiveOrder (strength: 0.42)
  ← InformationClarity (strength: 0.2)
  ← CircadianSupport (strength: 0.12)
  ← ContemplativeDepth (strength: 0.1)
  [computed in DependencyGraphComputer]
```

**Advantages:**
- Editable in inspector
- Self-documenting (see which layers affect which)
- Runtime modifiable
- Testable in isolation
- Nonlinear influence via curves

## Nonlinear Influence Curves

Instead of simple linear weights, use easing curves:

```csharp
dependency.influenceCurve = EasingCurve.EaseInOutCubic();
// S-curve: weak at extremes, strong in middle range
// Useful for threshold behaviors
```

**Common Patterns:**

1. **Linear influence**: `EasingCurve.Linear()` - direct proportional
2. **Threshold activation**: `EasingCurve.EaseInOutCubic()` - weak below 0.3, strong above 0.7
3. **Exponential decay**: Custom curve editor - influence fades with source value
4. **Spring-based**: `EasingCurve.Spring()` - bouncy, oscillating influence

## Validation & Stability

Phase 3 integrates Phase 2 precision system:

```csharp
// Automatic validation
AuraStateValidator.ValidateState(context);     // Check NaN/infinity
AuraStateValidator.EnforceConstraints(context); // Clamp to [0,1]

// Recovery from instability
if (AuraStateValidator.DetectInstability(context))
{
    context.CurrentIntensity = 0.5; // Reset to neutral
}
```

## Debugging

### Visualize Dependency Graph
```csharp
[ContextMenu("Debug: Print Dependency Graph")]
```

Output:
```
Dependency Graph Computation Order:
  1. Layer 0
  2. Layer 1
  3. Layer 2
  ... etc

=== Dependency Graph Visualization ===

Layer 0:
  ← (no incoming)
  → Layer 2 (strength: 0.3, metric: intensity)
  → Layer 5 (strength: 0.1, metric: energy)

Layer 1:
  ← (no incoming)
  → Layer 3 (strength: 0.2, metric: integrity)
  ... etc
```

### Monitor State Changes
Enable "Monitor States" on DependencyGraphDebugger to log significant changes:
```
Layer Emotional: Energy 0.620 → 0.782 (Δ0.162)
Layer Mental: Energy 0.540 → 0.698 (Δ0.158)
```

### Export State Snapshot
```csharp
[ContextMenu("Export State Snapshot")]
```

Captures all layer states at a moment in time for analysis.

## Runtime Modification

Modify dependencies while the system is running:

```csharp
// Add new dependency
var newDep = new LayerDependency(
    sourceLayerId: 0,
    targetLayerId: 5,
    strength: 0.2f
);
graphComputer.AddDependency(newDep);

// Remove dependency
graphComputer.RemoveDependency(newDep);

// Clear all dependencies for a layer
graphComputer.RemoveDependenciesForLayer(layerId);

// Query dependencies
var incoming = graphComputer.GetIncomingDependencies(layerId);
var outgoing = graphComputer.GetOutgoingDependencies(layerId);
```

## Performance

**Computation Cost:**
- Topology sort: O(V + E) once per profile change
- Cascade computation: O(V + E) per frame
- Validation: O(V) per frame
- **Total**: Minimal impact on modern hardware (~1-2ms for 7 layers)

**Optimization Tips:**
1. Minimize dependencies per layer (aim for ≤3 incoming)
2. Avoid cycles (topology sort detects them)
3. Use validation only in debug builds if needed
4. Cache intermediate values (already done in PrecisionCalculator)

## Comparison: Legacy vs. Phase 3

| Aspect | Legacy | Phase 3 |
|--------|--------|---------|
| **Weights** | Hardcoded constants | Editable dependencies |
| **Visibility** | Hidden in code | Visible in inspector |
| **Modifiability** | Code change required | Edit InteractionProfile |
| **Runtime change** | No | Yes (AddDependency/RemoveDependency) |
| **Cycles** | Can happen implicitly | Detected and warned |
| **Influence curves** | Linear only | Supports easing curves |
| **Testability** | Difficult | Easy (mock dependencies) |
| **Documentation** | In comments | Self-documenting graph |

## Next Steps (Phase 4)

Phase 4 will:
- Create generic base architecture (AuraFrameworkBase)
- Unify Biofield and Integrative frameworks
- Eliminate code duplication (~90% shared)
- Single maintenance point for both frameworks

---

**Phase 3 Summary**: Explicit, editable dependency graph replaces implicit calculations. Topology-ordered computation with validation and debugging tools. Foundation for adaptive, runtime-modifiable cascade behaviors.
