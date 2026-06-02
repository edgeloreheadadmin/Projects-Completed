# Qiskit Aer for Android

Cross-compiled quantum simulator for Android (arm64-v8a, API 24+).

Built from **Qiskit Aer 0.14.2** using **Android NDK r26d** (Clang 17).

## What's included

| File | Size | Description |
|------|------|-------------|
| `dist/lib/arm64-v8a/libaer_simulator.so` | ~5.6 MB | Main simulator (stripped) |
| `dist/lib/arm64-v8a/libomp.so` | ~1.2 MB | OpenMP runtime (NDK) |
| `dist/lib/arm64-v8a/libc++_shared.so` | ~1.8 MB | C++ standard library |
| `dist/java/com/qiskit/aer/AerSimulator.java` | — | JNI wrapper class |
| `dist/include/aer_runtime_api.h` | — | C API header |
| `dist/lib/arm64-v8a/libaer_simulator.debug.so` | ~261 MB | Unstripped (for debugging) |

## Quick start

### 1. Copy libraries into your Android project

```bash
ABI=arm64-v8a
PROJ=<your-android-project>

cp dist/lib/$ABI/libaer_simulator.so $PROJ/app/src/main/jniLibs/$ABI/
cp dist/lib/$ABI/libomp.so           $PROJ/app/src/main/jniLibs/$ABI/
cp dist/lib/$ABI/libc++_shared.so    $PROJ/app/src/main/jniLibs/$ABI/
cp dist/java/com/qiskit/aer/AerSimulator.java \
   $PROJ/app/src/main/java/com/qiskit/aer/
```

### 2. Example — Bell state circuit

```java
import com.qiskit.aer.AerSimulator;

// Create and configure the simulator
long state = AerSimulator.nativeCreateState();
AerSimulator.nativeConfigure(state, "method", "statevector");
AerSimulator.nativeInitialize(state);

// Allocate 2 qubits (returns index of first qubit)
long q0 = AerSimulator.nativeAllocateQubits(state, 2);
long q1 = q0 + 1;

// Build Bell state: H(q0) → CX(q0, q1)
AerSimulator.nativeH(state, q0);
AerSimulator.nativeCX(state, q0, q1);

// Measure both qubits
long outcome = AerSimulator.nativeMeasure(state, new long[]{q0, q1});
double prob   = AerSimulator.nativeProbability(state, outcome);

System.out.println("Outcome: " + outcome + "  probability: " + prob);
// → Outcome: 0 or 3  (|00⟩ or |11⟩), probability ≈ 0.5

// Clean up
AerSimulator.nativeFinalize(state);
```

### 3. Simulator methods

Configure the method before calling `nativeInitialize`:

| Method string | Description |
|---------------|-------------|
| `"statevector"` | Full statevector (default, exact) |
| `"density_matrix"` | Density matrix (with noise) |
| `"mps"` | Matrix Product State (low-entanglement circuits) |
| `"stabilizer"` | Clifford stabilizer (Clifford circuits only) |
| `"extended_stabilizer"` | Extended stabilizer |

### 4. Available gates

**Single-qubit:** H, X, Y, Z, S, Sdg, T, Tdg, SX, P(λ), RX(θ), RY(θ), RZ(θ), U3(θ,φ,λ)

**Two-qubit:** CX, CY, CZ, CP, CRX, CRY, CRZ, CH, SWAP, CU(θ,φ,λ,γ)

**Three-qubit:** CCX (Toffoli), CSWAP (Fredkin)

## Build from source

### Prerequisites

- Linux x86_64 host
- CMake ≥ 3.22, Ninja
- Java (any version, for NDK only)
- Network access (downloads NDK + source automatically)

### One-shot build

```bash
./build_all.sh
```

This downloads:
1. Android NDK r26d (~820 MB)
2. Qiskit Aer 0.14.2 source (~7 MB)
3. Eigen 3.4.0, nlohmann/json, spdlog, muparserx (headers)

Total build time: ~5 min on a 4-core machine.

### Manual build steps

```bash
bash scripts/01_download_ndk.sh
bash scripts/02_download_aer.sh
bash scripts/03_build_openblas.sh   # optional, improves GEMM performance
bash scripts/04_build_aer_android.sh
```

## Architecture notes

- **BLAS/LAPACK**: Uses Eigen's built-in BLAS/LAPACK back-end. No external OpenBLAS required.
- **OpenMP**: Linked against NDK's `libomp.so` (OpenMP 5.0).
- **No Python**: The JNI bridge calls Aer's native C API (`aer_runtime_api.h`) directly.
- **No CUDA / GPU**: CPU-only build. Thrust GPU backend disabled.
- **Target**: `arm64-v8a` (ARMv8-A, 64-bit). All modern Android phones (2016+).

## License

Qiskit Aer is licensed under the Apache License 2.0.  
Eigen is licensed under MPL2.  
This build system is provided under Apache License 2.0.
