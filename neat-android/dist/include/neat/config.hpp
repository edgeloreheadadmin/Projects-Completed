#pragma once
// config.hpp — NEAT hyperparameter POD struct
// All tunable constants for the NEAT algorithm.

#include <cstdint>
#include <string>

namespace neat {

/// Activation function identifiers
enum class Activation : uint8_t {
    SIGMOID = 0,
    TANH    = 1,
    RELU    = 2,
    LINEAR  = 3,
};

/// Convert activation name string to enum
inline Activation activation_from_string(const std::string& s) {
    if (s == "tanh")    return Activation::TANH;
    if (s == "relu")    return Activation::RELU;
    if (s == "linear")  return Activation::LINEAR;
    return Activation::SIGMOID;
}

inline const char* activation_to_string(Activation a) {
    switch (a) {
        case Activation::TANH:    return "tanh";
        case Activation::RELU:    return "relu";
        case Activation::LINEAR:  return "linear";
        default:                  return "sigmoid";
    }
}

struct Config {
    // ── Population ─────────────────────────────────────────────────────────
    int   population_size        = 150;
    int   num_inputs             = 2;
    int   num_outputs            = 1;

    // ── Compatibility / Speciation ──────────────────────────────────────────
    double compat_threshold      = 3.0;   ///< δ < threshold → same species
    double c1                    = 1.0;   ///< excess gene coefficient
    double c2                    = 1.0;   ///< disjoint gene coefficient
    double c3                    = 0.4;   ///< weight difference coefficient
    int    stagnation_limit      = 15;    ///< cull species after N stagnant gens

    // ── Mutation probabilities ─────────────────────────────────────────────
    double prob_add_node         = 0.03;
    double prob_add_conn         = 0.05;
    double prob_mutate_weights   = 0.80;
    double prob_perturb          = 0.90;  ///< perturb vs. replace weight
    double perturb_power         = 0.5;   ///< std-dev of weight perturbation
    double weight_range          = 4.0;   ///< random weight uniform range
    double prob_toggle_enable    = 0.01;

    // ── Crossover ─────────────────────────────────────────────────────────
    double prob_crossover        = 0.75;  ///< fraction of offspring via crossover
    double prob_interspecies     = 0.001; ///< interspecies mating rate

    // ── Selection ─────────────────────────────────────────────────────────
    double survival_threshold    = 0.20;  ///< top fraction allowed to reproduce
    int    elitism               = 1;     ///< number of elites copied verbatim

    // ── Network ───────────────────────────────────────────────────────────
    Activation default_activation = Activation::SIGMOID;
    double     bias_value         = 1.0;

    // ── Seed ──────────────────────────────────────────────────────────────
    uint64_t seed                = 0;     ///< 0 = non-deterministic
};

} // namespace neat
