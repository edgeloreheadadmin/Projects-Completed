package com.qiskit.aer;

/**
 * Android JNI wrapper for the Qiskit Aer quantum statevector simulator.
 *
 * <p>Each simulation session is represented by a native "state" handle.
 * Example – Bell state circuit:
 * <pre>
 *   long state = AerSimulator.nativeCreateState();
 *   AerSimulator.nativeConfigure(state, "method", "statevector");
 *   AerSimulator.nativeInitialize(state);
 *   long q0 = AerSimulator.nativeAllocateQubits(state, 2);
 *   long q1 = q0 + 1;
 *   AerSimulator.nativeH(state, q0);
 *   AerSimulator.nativeCX(state, q0, q1);
 *   long outcome = AerSimulator.nativeMeasure(state, new long[]{q0, q1});
 *   System.out.println("Measurement: " + outcome + " p=" + AerSimulator.nativeProbability(state, outcome));
 *   AerSimulator.nativeFinalize(state);
 * </pre>
 */
public class AerSimulator {

    static {
        System.loadLibrary("aer_simulator");
    }

    // ── State lifecycle ────────────────────────────────────────────────────

    /** Allocates a new Aer simulation state. Returns an opaque native handle. */
    public static native long nativeCreateState();

    /** Initializes the state (must be called after configure, before gate ops). */
    public static native void nativeInitialize(long stateHandle);

    /** Releases all native resources for this state. */
    public static native void nativeFinalize(long stateHandle);

    /**
     * Sets a simulator configuration option.
     * Common keys: "method" (statevector|density_matrix|mps|stabilizer),
     *              "precision" (double|single), "max_parallel_threads".
     */
    public static native void nativeConfigure(long stateHandle, String key, String value);

    // ── Qubit allocation ───────────────────────────────────────────────────

    /**
     * Allocates {@code numQubits} qubits and returns the index of the first one.
     * Subsequent qubits are at firstIndex+1, firstIndex+2, etc.
     */
    public static native long nativeAllocateQubits(long stateHandle, long numQubits);

    // ── Measurement ────────────────────────────────────────────────────────

    /** Measures the given qubits and returns the integer outcome. */
    public static native long nativeMeasure(long stateHandle, long[] qubits);

    /** Returns the probability of a specific measurement outcome. */
    public static native double nativeProbability(long stateHandle, long outcome);

    // ── Single-qubit gates ─────────────────────────────────────────────────

    public static native void nativeH(long h, long qubit);
    public static native void nativeX(long h, long qubit);
    public static native void nativeY(long h, long qubit);
    public static native void nativeZ(long h, long qubit);
    public static native void nativeS(long h, long qubit);
    public static native void nativeSdg(long h, long qubit);
    public static native void nativeT(long h, long qubit);
    public static native void nativeTdg(long h, long qubit);
    public static native void nativeSX(long h, long qubit);
    public static native void nativeU3(long h, long qubit, double theta, double phi, double lambda);
    public static native void nativeP(long h, long qubit, double lambda);
    public static native void nativeRX(long h, long qubit, double theta);
    public static native void nativeRY(long h, long qubit, double theta);
    public static native void nativeRZ(long h, long qubit, double theta);

    // ── Two-qubit gates ────────────────────────────────────────────────────

    public static native void nativeCX(long h, long ctrl, long tgt);
    public static native void nativeCY(long h, long ctrl, long tgt);
    public static native void nativeCZ(long h, long ctrl, long tgt);
    public static native void nativeCP(long h, long ctrl, long tgt, double lambda);
    public static native void nativeCRX(long h, long ctrl, long tgt, double theta);
    public static native void nativeCRY(long h, long ctrl, long tgt, double theta);
    public static native void nativeCRZ(long h, long ctrl, long tgt, double theta);
    public static native void nativeCH(long h, long ctrl, long tgt);
    public static native void nativeSWAP(long h, long q0, long q1);
    public static native void nativeCU(long h, long ctrl, long tgt,
                                       double theta, double phi, double lambda, double gamma);

    // ── Three-qubit gates ──────────────────────────────────────────────────

    public static native void nativeCCX(long h, long q0, long q1, long q2);
    public static native void nativeCSWAP(long h, long ctrl, long q0, long q1);

    // ── Meta ───────────────────────────────────────────────────────────────

    /** Returns the build version string. */
    public static native String nativeVersion();
}
