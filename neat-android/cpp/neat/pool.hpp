#pragma once
// pool.hpp — population manager: selection, speciation, reproduction

#include "config.hpp"
#include "genome.hpp"
#include "species.hpp"
#include "network.hpp"
#include <vector>
#include <memory>
#include <random>
#include <string>

namespace neat {

struct PoolStats {
    int    generation        = 0;
    int    population_size   = 0;
    int    num_species       = 0;
    double best_fitness      = 0.0;
    double avg_fitness       = 0.0;
    int    best_genome_id    = -1;
};

class Pool {
public:
    explicit Pool(const Config& cfg);

    // ── Per-generation interface ──────────────────────────────────────────────

    /// Total number of genomes in the current generation.
    int  population_size() const { return static_cast<int>(genomes_.size()); }

    /// Access a genome by population index (0-based).
    Genome&       genome(int idx)       { return genomes_[idx]; }
    const Genome& genome(int idx) const { return genomes_[idx]; }

    /// Set fitness for genome at index idx.
    void set_fitness(int idx, double f) { genomes_[idx].set_fitness(f); }

    /// Build a Network for genome at idx (for inference).
    Network make_network(int idx) const;

    /// Advance: speciate current population, compute fitness sharing,
    /// produce next generation, increment generation counter.
    void next_generation();

    // ── Stats ─────────────────────────────────────────────────────────────────
    PoolStats stats() const;

    /// Serialise the best genome to a compact JSON string.
    std::string best_genome_json() const;

    int generation() const { return generation_; }

private:
    Config             cfg_;
    std::mt19937_64    rng_;
    InnovationTracker  tracker_;

    std::vector<Genome>             genomes_;
    std::vector<std::unique_ptr<Species>> species_;

    int generation_   = 0;
    int next_genome_id_ = 0;
    int next_species_id_ = 0;

    double global_best_fitness_ = 0.0;
    int    best_genome_idx_     = 0;

    // ── Internal helpers ──────────────────────────────────────────────────────
    void speciate();
    void compute_fitness();
    void reproduce();
    Genome breed_offspring(int new_id);

    Genome& random_member_of(Species& sp);
    int  new_genome_id()  { return next_genome_id_++; }
    int  new_species_id() { return next_species_id_++; }
};

} // namespace neat
