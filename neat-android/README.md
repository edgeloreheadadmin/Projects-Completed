# NEAT Neuroevolution for Android

A from-scratch C++ implementation of the **NEAT** (NeuroEvolution of Augmenting Topologies)
algorithm, cross-compiled for Android arm64-v8a with a JNI wrapper for Java/Kotlin.

Built with **Android NDK r26d** (Clang 17), no external dependencies.

## What's included

| File | Size | Description |
|------|------|-------------|
| `dist/lib/arm64-v8a/libneat.so` | ~116 KB | NEAT simulator (stripped) |
| `dist/lib/arm64-v8a/libc++_shared.so` | ~1.8 MB | C++ runtime (NDK) |
| `dist/java/com/qiskit/neat/NeatEvolver.java` | — | JNI wrapper class |
| `dist/include/neat/*.hpp` | — | C++ headers |

## Quick start

### 1. Copy into your Android project

```bash
ABI=arm64-v8a
PROJ=<your-android-project>

cp dist/lib/$ABI/libneat.so         $PROJ/app/src/main/jniLibs/$ABI/
cp dist/lib/$ABI/libc++_shared.so   $PROJ/app/src/main/jniLibs/$ABI/
cp dist/java/com/qiskit/neat/NeatEvolver.java \
   $PROJ/app/src/main/java/com/qiskit/neat/
```

### 2. Evolve XOR

```java
import com.qiskit.neat.NeatEvolver;

// Create a population: 2 inputs, 1 output, 150 genomes
long pool = NeatEvolver.nativeCreate(2, 1, 150, "{}");

double[][] cases   = {{0,0},{0,1},{1,0},{1,1}};
double[]   targets = { 0.0,  1.0,  1.0,  0.0};

for (int gen = 0; gen < 300; gen++) {
    int n = NeatEvolver.nativeGetPopulationSize(pool);

    for (int i = 0; i < n; i++) {
        double err = 0;
        for (int c = 0; c < 4; c++) {
            double[] out = NeatEvolver.nativeActivate(pool, i, cases[c]);
            double d = out[0] - targets[c];
            err += d * d;
        }
        // Fitness: maximise (NEAT requires non-negative, higher = better)
        NeatEvolver.nativeSetFitness(pool, i, Math.pow(4.0 - err, 2));
    }

    NeatEvolver.nativeNextGeneration(pool);
    System.out.println("Gen " + gen + ": " + NeatEvolver.nativeGetStats(pool));
}

String best = NeatEvolver.nativeGetBestGenome(pool);
NeatEvolver.nativeDestroy(pool);
```

## API reference

| Method | Description |
|--------|-------------|
| `nativeCreate(in, out, pop, configJson)` | Create population; returns handle |
| `nativeDestroy(handle)` | Free native memory |
| `nativeSetFitness(handle, idx, f)` | Set fitness for genome `idx` |
| `nativeNextGeneration(handle)` | Speciate → reproduce → mutate |
| `nativeActivate(handle, idx, inputs[])` | Run network; returns `outputs[]` |
| `nativeGetStats(handle)` | JSON: generation, species, best/avg fitness |
| `nativeGetPopulationSize(handle)` | Current population count |
| `nativeGetBestGenome(handle)` | JSON: nodes + connections of best genome |
| `nativeGetVersion()` | Version string |

## Config JSON defaults

```json
{
  "compat_threshold":   3.0,
  "c1": 1.0, "c2": 1.0, "c3": 0.4,
  "stagnation_limit":   15,
  "prob_add_node":      0.03,
  "prob_add_conn":      0.05,
  "prob_mutate_weights": 0.80,
  "prob_perturb":       0.90,
  "perturb_power":      0.5,
  "weight_range":       4.0,
  "prob_crossover":     0.75,
  "survival_threshold": 0.20,
  "elitism":            1,
  "activation":         "sigmoid",
  "seed":               0
}
```

## Build from source

```bash
# NDK is shared with qiskit-aer-android — run its setup first if needed:
# cd ../qiskit-aer-android && bash scripts/01_download_ndk.sh

bash scripts/01_build_neat_android.sh
```

## Algorithm notes

- **Genomes** encode nodes (INPUT / HIDDEN / OUTPUT / BIAS) and directed weighted connections, each tagged with a global innovation number for historical alignment.
- **Speciation** uses genomic distance δ = (c1·E + c2·D)/N + c3·W̄ to group similar genomes and protect innovation.
- **Reproduction** is proportional to adjusted (fitness-shared) fitness per species; stagnant species are culled after `stagnation_limit` generations.
- **Crossover** aligns genes by innovation number; disjoint/excess genes are inherited from the fitter parent.
- **Mutations**: perturb/replace weights, add connection (feed-forward), split connection with a new node, toggle enable/disable.
- **Network evaluation**: topological sort (Kahn's algorithm) at construction; O(E) activation per forward pass.

## License

Apache License 2.0
