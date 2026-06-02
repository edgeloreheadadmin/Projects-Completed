package com.qiskit.neat;

/**
 * Android JNI wrapper for the NEAT (NeuroEvolution of Augmenting Topologies)
 * neuroevolution library.
 *
 * <p>Example — XOR evolution:
 * <pre>
 *   long pool = NeatEvolver.nativeCreate(2, 1, 150, "{}");
 *
 *   for (int gen = 0; gen < 300; gen++) {
 *       int n = NeatEvolver.nativeGetPopulationSize(pool);
 *       double[][] cases = {{0,0},{0,1},{1,0},{1,1}};
 *       double[] expected  = {0.0, 1.0, 1.0, 0.0};
 *
 *       for (int i = 0; i < n; i++) {
 *           double error = 0;
 *           for (int c = 0; c < 4; c++) {
 *               double[] out = NeatEvolver.nativeActivate(pool, i, cases[c]);
 *               double d = out[0] - expected[c];
 *               error += d * d;
 *           }
 *           NeatEvolver.nativeSetFitness(pool, i, (4.0 - error) * (4.0 - error));
 *       }
 *       NeatEvolver.nativeNextGeneration(pool);
 *       System.out.println(NeatEvolver.nativeGetStats(pool));
 *   }
 *   NeatEvolver.nativeDestroy(pool);
 * </pre>
 *
 * <p>Config JSON keys (all optional, defaults shown):
 * <pre>
 *   {
 *     "compat_threshold":  3.0,
 *     "c1": 1.0, "c2": 1.0, "c3": 0.4,
 *     "stagnation_limit":  15,
 *     "prob_add_node":     0.03,
 *     "prob_add_conn":     0.05,
 *     "prob_mutate_weights": 0.80,
 *     "prob_perturb":      0.90,
 *     "perturb_power":     0.5,
 *     "weight_range":      4.0,
 *     "prob_crossover":    0.75,
 *     "survival_threshold": 0.20,
 *     "elitism":           1,
 *     "activation":        "sigmoid",   // sigmoid|tanh|relu|linear
 *     "seed":              0            // 0 = non-deterministic
 *   }
 * </pre>
 */
public class NeatEvolver {

    static {
        System.loadLibrary("neat");
    }

    /**
     * Create a new NEAT population.
     *
     * @param inputs       number of input neurons
     * @param outputs      number of output neurons
     * @param populationSize number of genomes per generation
     * @param configJson   JSON string with optional hyperparameter overrides
     * @return opaque native handle (must be freed with {@link #nativeDestroy})
     */
    public static native long nativeCreate(int inputs, int outputs,
                                           int populationSize, String configJson);

    /** Free all native resources for this pool. */
    public static native void nativeDestroy(long handle);

    /**
     * Set the fitness score for genome {@code genomeIdx} in the current
     * generation.  Call this for every genome before calling
     * {@link #nativeNextGeneration}.
     */
    public static native void nativeSetFitness(long handle, int genomeIdx, double fitness);

    /**
     * Advance to the next generation: speciate, apply fitness sharing,
     * reproduce, mutate.  Resets all fitness values to 0.
     */
    public static native void nativeNextGeneration(long handle);

    /**
     * Activate genome {@code genomeIdx}'s network with the given inputs and
     * return the output activations.
     *
     * @param inputs array of length equal to the number of inputs given at
     *               {@link #nativeCreate}
     * @return output activations array (length = number of outputs)
     */
    public static native double[] nativeActivate(long handle, int genomeIdx,
                                                 double[] inputs);

    /**
     * Return current generation statistics as a JSON string.
     * Keys: generation, population, num_species, best_fitness, avg_fitness,
     *       best_genome_id.
     */
    public static native String nativeGetStats(long handle);

    /** Return the number of genomes in the current generation. */
    public static native int nativeGetPopulationSize(long handle);

    /**
     * Serialize the best genome to JSON (nodes + connections + weights).
     */
    public static native String nativeGetBestGenome(long handle);

    /** Returns the native library version string. */
    public static native String nativeGetVersion();
}
