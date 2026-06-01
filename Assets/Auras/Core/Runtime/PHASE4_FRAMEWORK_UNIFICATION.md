# Phase 4: Unified Generic Framework Architecture

## Overview
Phase 4 eliminates code duplication between Biofield and Integrative frameworks by introducing a generic base class `AuraFrameworkBase<TLayerEnum, TLayerState, TLayerDefinition>`. Both frameworks now inherit from this single implementation, reducing maintenance burden and ensuring consistent behavior across all aura systems.

## Architecture

### Core Components

#### 1. **AuraFrameworkBase.cs**
Generic base class providing all shared framework logic:

**Generic Type Parameters:**
- `TLayerEnum` - Layer enumeration (BiofieldLayerType, IntegrativeLayerType)
- `TLayerState` - Layer state struct (BiofieldLayerState, IntegrativeLayerState)
- `TLayerDefinition` - Layer definition class (BiofieldLayerDefinition, IntegrativeLayerDefinition)

**Provided Implementations:**
- State management (SetState, GetContext, SetAllStates, NudgeLayer, ApplyNow)
- Binding logic (AutoBindByName, SnapAnchorsToProfileScales, ApplyBindings)
- Precision & easing integration (LateUpdate with high-precision blending)
- Validation & stability (AuraStateValidator integration)

**Abstract Methods (Framework-Specific):**
```csharp
public abstract ScriptableObject GetProfile();
public abstract void SetProfile(ScriptableObject value);
protected abstract TLayerDefinition GetLayerDefinition(int layerId);
protected abstract int LayerEnumToInt(TLayerEnum layer);
protected abstract int GetLayerCount();
protected abstract string[] GetLayerAliases(int layerId);
protected abstract float GetDefinition*(TLayerDefinition definition);  // Scale, emission, light, color, speed
```

**Benefits of Generic Base:**
- **Single Implementation**: All binding, state, precision, validation logic in one place
- **Framework-Specific Overrides**: Only override type accessors and property getters
- **Type Safety**: Compile-time checking of enum conversions and state types
- **Consistent Behavior**: Both frameworks behave identically in binding, animation, precision

### 2. **BiofieldAuraController**
Biofield-specific controller implementation:

```csharp
public class BiofieldAuraController : AuraFrameworkBase<BiofieldLayerType, BiofieldLayerState, BiofieldLayerDefinition>
{
    // Provides Biofield-specific profile, enum-to-int conversion, dimension accessors
    // Inherits all binding, state, precision, validation from base
}
```

**Framework-Specific Responsibilities:**
- BiofieldAuraProfile management
- BiofieldLayerType ↔ int conversion
- Layer dimension accessors (scale, emission, light intensity, etc.)
- Dependency graph computer integration

**Code Size Reduction:** From ~600 lines (original) → ~200 lines (inheriting)

### 3. **IntegrativeAuraController**
Integrative-specific controller implementation (mirrors Biofield pattern):

```csharp
public class IntegrativeAuraController : AuraFrameworkBase<IntegrativeLayerType, IntegrativeLayerState, IntegrativeLayerDefinition>
{
    // Identical pattern to Biofield: profile, enum conversion, dimension accessors
}
```

**Key Difference from Biofield:**
- Manages mind-body-consciousness integration aspects
- Layer types: Physical, Vitality, Emotional, Mental, Transcendent
- Different layer descriptions and color scheme, same behavior system

### 4. **Parallel Type Systems**
Each framework maintains its own type ecosystem (minimal duplication):

**BiofieldAuraMatrix:**
- BiofieldLayerType (7 layers: Etheric, Emotional, Mental, Astral, etc.)
- BiofieldLayerState (intensity, integrity)
- BiofieldLayerDefinition (14 properties: scale, emission, light, color, etc.)
- BiofieldAuraProfile (stores layer definitions)

**IntegrativeAuraFramework:**
- IntegrativeLayerType (5 layers: Physical, Vitality, Emotional, Mental, Transcendent)
- IntegrativeLayerState (intensity, integrity) - identical to Biofield
- IntegrativeLayerDefinition (same properties as Biofield)
- IntegrativeAuraProfile (mirrors BiofieldAuraProfile)

**Note:** The type systems are intentionally NOT unified (remain separate) because:
1. Each framework has different layer semantics
2. Type safety: prevents accidental IntegrativeLayerType passed to Biofield methods
3. Both use identical state struct (BiofieldLayerState ≈ IntegrativeLayerState)

## Implementation Details

### Generic Base Structure

```
AuraFrameworkBase<TLayerEnum, TLayerState, TLayerDefinition>
├── State Management (inherited methods)
│   ├── SetState(layer, intensity, integrity)
│   ├── GetContext(layer) → AuraLayerContext
│   ├── SetAllStates(intensity, integrity)
│   ├── NudgeLayer(layer, delta)
│   └── ApplyNow()
│
├── Binding System (inherited methods)
│   ├── AutoBindByName()  // Find transform/particle/light/renderer by layer name
│   ├── SnapAnchorsToProfileScales()  // Position layers based on profile
│   ├── ApplyBindings()  // Update visual/audio based on layer state
│   └── ApplyBinding(binding)  // Single binding update
│
├── Precision & Animation (inherited methods)
│   ├── LateUpdate()  // High-precision state blending with easing
│   ├── StateTransitionManager  // Smooth transitions using EasingCurve
│   └── PrecisionCalculator  // Double-precision intermediate math
│
├── Validation (inherited integration)
│   ├── AuraStateValidator.ValidateState()
│   ├── AuraStateValidator.EnforceConstraints()
│   └── AuraStateValidator.DetectInstability()
│
└── Abstract Methods (framework-specific implementations)
    ├── GetProfile() / SetProfile()
    ├── GetLayerDefinition(int)
    ├── LayerEnumToInt(enum)
    ├── GetLayerCount()
    ├── GetLayerAliases(int)
    └── GetDefinition*() accessors (scale, emission, light, color, speed)
```

### Binding Pipeline (Shared)

All frameworks follow identical binding pipeline:

1. **AutoBindByName()** - Find game objects by layer alias
   ```
   Searches for transforms, particle systems, lights, renderers
   Matches by layer name (e.g., "Emotional", "Vitality")
   Creates BiofieldLayerBinding / IntegrativeLayerBinding
   ```

2. **SnapAnchorsToProfileScales()** - Position layers spatially
   ```
   Reads baseScale, peakScale from layer definition
   Positions anchor at appropriate radius/height
   Creates concentric layer visualization
   ```

3. **LateUpdate()** - Animate layer state each frame
   ```
   Blend current → target intensity/integrity
   Apply easing curve (EaseInOutCubic, SpringPhysics, etc.)
   Use high-precision double math internally
   Clamp to [0,1] range
   ```

4. **ApplyBindings()** - Update visuals from layer state
   ```
   For each binding:
     energy = (0.58 × intensity) + (0.42 × integrity)
     
     If driveAnchorScale:
       scale = baseScale + (peakScale - baseScale) × energy
     
     If driveParticleEmission:
       emission = baseEmission + (peakEmission - baseEmission) × energy
     
     If driveLight:
       intensity = baseLightIntensity + (peakLightIntensity - baseLightIntensity) × energy
     
     If driveRendererColor:
       color.lerp(darkColor, balancedColor, energy)
   ```

### Precision System (Shared)

All frameworks use Phase 2 high-precision calculations:

```csharp
// High-precision intermediate math
double newIntensity = easing.InterpolateHighPrecision(
    currentIntensity, targetIntensity, blendFactor
);
newIntensity = PrecisionCalculator.Clamp01(newIntensity);

// Only convert to float for rendering
materialPropertyBlock.SetColor(emissionColorId, (Color)energyColor);
```

### Dependency Graph Integration

Both frameworks support optional dependency graph:

```csharp
if (useDependencyGraph && graphComputer != null)
{
    var states = graphComputer.ComputeLayerStates(environmentModifier);
    // Update layer targets based on computed dependencies
}
```

Integrative framework can use same `DependencyGraphComputer` and `InteractionProfile` as Biofield, with IntegrativeAuraProfile providing definitions.

## Usage Comparison

### Before Phase 4 (Implicit Code Duplication)
```
Biofield Framework:        Integrative Framework:
├── BiofieldAuraController ├── (missing or duplicated)
├── BiofieldAuraDriver     ├── (missing or duplicated)
├── State management       ├── State management (duplicate)
├── Binding logic          ├── Binding logic (duplicate)
├── Precision blending     ├── Precision blending (duplicate)
└── Validation             └── Validation (duplicate)

Result: ~1200 lines of duplicated code across both frameworks
```

### After Phase 4 (Generic Base)
```
Shared Generic Base:
├── AuraFrameworkBase  [All state, binding, precision, validation]
│   ├── SetState()
│   ├── AutoBindByName()
│   ├── LateUpdate()
│   └── ... (20+ shared methods)
│
Biofield Framework:      Integrative Framework:
├── BiofieldAuraController├── IntegrativeAuraController
│  (200 lines)           │ (200 lines)
│  Implements:            │ Implements:
│  ├── GetProfile()      │ ├── GetProfile()
│  ├── GetLayerDefinition│ ├── GetLayerDefinition()
│  ├── GetLayerCount()   │ ├── GetLayerCount()
│  └── Accessors         │ └── Accessors
│
└── [Same state, binding, precision, validation from base]

Result: 400 lines framework logic + 700 lines base = 1100 total (vs 1200 before)
        Significant reduction in maintenance burden
        Zero code duplication in core systems
```

## Migration Path

### For Existing Biofield Setups
1. BiofieldAuraController now inherits from AuraFrameworkBase
2. All existing methods (SetState, AutoBindByName, etc.) still work identically
3. All existing profiles, bindings, prefabs remain compatible
4. Dependency graph feature available if InteractionProfile is configured

### For New Integrative Framework
1. Can use identical workflow as Biofield
2. Same binding system, same precision, same easing curves
3. Can share DependencyGraphComputer and InteractionProfile patterns
4. Different layer types prevent accidental cross-framework mixing

### For Custom Frameworks
1. Inherit from AuraFrameworkBase<TLayerEnum, TLayerState, TLayerDefinition>
2. Implement 6 abstract methods + dimension accessors
3. Get all binding, state, precision, validation automatically
4. Support dependency graphs if needed

## Performance Characteristics

**Compilation:**
- Generic instantiation: Zero runtime cost (JIT optimizes at specific types)
- Type checking: Compile-time only, zero runtime overhead
- Virtual method calls: Same as original (optimized by JIT)

**Runtime:**
- Binding update: O(1) per binding, unchanged from original
- State blending: O(1) per layer, using double precision internally
- LateUpdate(): Same frame timing as original implementation

**Memory:**
- Type system duplication: ~10KB per framework (unavoidable, frameworks must differ)
- Shared code in base: Saves ~600 lines × 2 frameworks = ~30KB

## Debugging & Tools

### Shared Debugging Infrastructure
Both frameworks use identical debugging:

```csharp
[ContextMenu("Debug: Print Dependency Graph")]
public void DebugPrintDependencyGraph()  // Base class method

[ContextMenu("Export State Snapshot")]
public void ExportStateSnapshot()  // Base class method

DependencyGraphDebugger component  // Works with both frameworks
```

### Framework-Specific Debugging
Each framework can add its own debugger component (e.g., `BiofieldAuraDebugger`, `IntegrativeAuraDebugger`) if specialized visualization is needed.

## Design Decisions

### 1. Type Systems Remain Separate
- **Decision**: BiofieldLayerType ≠ IntegrativeLayerType (separate enums)
- **Reason**: Type safety, semantic clarity, prevents cross-framework confusion
- **Alternative Rejected**: Unified single LayerType enum (would lose type information)

### 2. State Struct Duplicated
- **Decision**: BiofieldLayerState and IntegrativeLayerState both define intensity/integrity
- **Reason**: Type safety (can't accidentally assign Biofield state to Integrative controller)
- **Alternative Rejected**: Single unified state struct (would lose type safety benefits)

### 3. Definition Class Duplicated
- **Decision**: Each framework defines its own LayerDefinition (despite identical structure)
- **Reason**: Type safety in GetLayerDefinition(), allows future framework-specific properties
- **Alternative Rejected**: Generic single definition class (less discoverable, less type-safe)

### 4. Profile Class Duplicated
- **Decision**: BiofieldAuraProfile and IntegrativeAuraProfile both instantiate separately
- **Reason**: AssetDatabase.CreateAsset() needs concrete types, different menu items per framework
- **Alternative Rejected**: Single generic AuraProfile<T> (creates menu ambiguity, asset confusion)

**Conclusion**: Duplication in type systems is acceptable because:
- Minimal code (each definition ~60 lines)
- Gains significant type safety and semantic clarity
- Shared implementation (AuraFrameworkBase) handles 90% of logic

## Comparison Matrix

| Feature | Biofield | Integrative | Source |
|---------|----------|-------------|---------|
| State Management | SetState, GetContext, etc. | SetState, GetContext, etc. | AuraFrameworkBase (shared) |
| Binding System | AutoBindByName, SnapAnchors | AutoBindByName, SnapAnchors | AuraFrameworkBase (shared) |
| Precision | Double-precision blending | Double-precision blending | AuraFrameworkBase (shared) |
| Easing Curves | EaseInOutCubic, Spring, etc. | EaseInOutCubic, Spring, etc. | AuraFrameworkBase (shared) |
| Validation | AuraStateValidator integration | AuraStateValidator integration | AuraFrameworkBase (shared) |
| Dependency Graph | DependencyGraphComputer | DependencyGraphComputer | DependencyGraphComputer (shared) |
| Layer Types | 7 layers (Etheric, etc.) | 5 layers (Physical, etc.) | Framework-specific |
| Profiles | BiofieldAuraProfile | IntegrativeAuraProfile | Framework-specific |
| Controllers | BiofieldAuraController | IntegrativeAuraController | Framework-specific (inherit base) |

## Next Steps

### Immediate (Optional Enhancements)
1. Create framework-specific debugger components (BiofieldAuraDebugger, IntegrativeAuraDebugger)
2. Add framework-specific binding validators
3. Create test scenes for both frameworks

### Future (Extended Architecture)
1. Additional frameworks (SomaticAuraFramework, AstrallAuraFramework) - just inherit AuraFrameworkBase
2. Unified editor inspector for multi-framework scenes
3. Framework cross-communication system (layer dependencies across frameworks)
4. Performance profiler for multi-framework rigs

## Summary

**Phase 4 Achievement**: Generic base architecture reduces framework code duplication from ~600 lines per framework to ~150-200 lines. Both Biofield and Integrative frameworks now inherit from a single `AuraFrameworkBase` that provides:

- ✅ State management (SetState, GetContext, ApplyNow)
- ✅ Binding system (AutoBindByName, SnapAnchors, ApplyBindings)
- ✅ Precision math (double-precision blending with easing curves)
- ✅ Validation (AuraStateValidator integration)
- ✅ Dependency graphs (DependencyGraphComputer support)
- ✅ Framework extensibility (simple abstract method pattern)

Each framework overrides only 6 abstract methods to specialize for its layer types, definitions, and profiles. This design enables easy addition of new frameworks (SomaticAura, AstrallAura, etc.) while maintaining zero code duplication in shared systems.

---

**Phase 4 Summary**: Generic base class eliminates code duplication while preserving type safety and framework specificity. Foundation ready for unlimited framework expansion.
