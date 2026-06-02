#pragma once
// genome.hpp — NodeGene, ConnectionGene, Genome with innovation numbers

#include "config.hpp"
#include <vector>
#include <unordered_map>
#include <cstdint>
#include <atomic>
#include <utility>
#include <memory>
#include <random>

namespace neat {

// ── Innovation tracker ────────────────────────────────────────────────────────
/// Maintains a global innovation counter and maps (in,out) → innovation_id
/// so that structural mutations that occur in the same generation share IDs.
class InnovationTracker {
public:
    InnovationTracker() : next_innovation_(1), next_node_id_(1) {}

    /// Return the innovation id for a connection; create one if new.
    int get_or_create_connection(int in_node, int out_node);

    /// Reserve the next node id.
    int new_node_id();

    /// Called at end of generation: flush the within-generation mutation map.
    void new_generation();

    int current_innovation() const { return next_innovation_ - 1; }
    int current_node_id()    const { return next_node_id_   - 1; }

    void set_next_node_id(int n)    { next_node_id_    = n; }
    void set_next_innovation(int n) { next_innovation_ = n; }

private:
    int next_innovation_;
    int next_node_id_;
    // within-generation map: (in,out) → innovation_id
    std::unordered_map<uint64_t, int> gen_map_;

    static uint64_t key(int in, int out) {
        return (static_cast<uint64_t>(in) << 32) | static_cast<uint32_t>(out);
    }
};

// ── Node types ────────────────────────────────────────────────────────────────
enum class NodeType : uint8_t {
    INPUT  = 0,
    HIDDEN = 1,
    OUTPUT = 2,
    BIAS   = 3,
};

// ── NodeGene ──────────────────────────────────────────────────────────────────
struct NodeGene {
    int        id;
    NodeType   type;
    Activation activation;

    NodeGene() : id(0), type(NodeType::HIDDEN), activation(Activation::SIGMOID) {}
    NodeGene(int id, NodeType t, Activation a)
        : id(id), type(t), activation(a) {}
};

// ── ConnectionGene ────────────────────────────────────────────────────────────
struct ConnectionGene {
    int    in_node;
    int    out_node;
    double weight;
    bool   enabled;
    int    innovation_id;

    ConnectionGene()
        : in_node(0), out_node(0), weight(0.0), enabled(true), innovation_id(0) {}
    ConnectionGene(int in, int out, double w, bool en, int innov)
        : in_node(in), out_node(out), weight(w), enabled(en), innovation_id(innov) {}
};

// ── Genome ────────────────────────────────────────────────────────────────────
class Genome {
public:
    Genome() = default;
    explicit Genome(int id) : id_(id) {}

    // ── Accessors ──────────────────────────────────────────────────────────
    int  id()      const { return id_; }
    void set_id(int id)  { id_ = id; }

    double fitness()          const { return fitness_; }
    void   set_fitness(double f)    { fitness_ = f; }
    double adjusted_fitness() const { return adjusted_fitness_; }
    void   set_adjusted_fitness(double f) { adjusted_fitness_ = f; }

    const std::vector<NodeGene>&       nodes()       const { return nodes_; }
    const std::vector<ConnectionGene>& connections() const { return connections_; }
    std::vector<NodeGene>&       nodes()             { return nodes_; }
    std::vector<ConnectionGene>& connections()       { return connections_; }

    // ── Construction helpers ───────────────────────────────────────────────
    void add_node(const NodeGene& ng);
    void add_connection(const ConnectionGene& cg);

    /// Build a minimal fully-connected input→output genome
    static Genome create_minimal(int genome_id,
                                 int num_inputs,
                                 int num_outputs,
                                 bool add_bias,
                                 const Config& cfg,
                                 InnovationTracker& tracker);

    // ── Compatibility distance ─────────────────────────────────────────────
    /// δ = (c1·E + c2·D)/N + c3·W̄
    static double compatibility(const Genome& a,
                                const Genome& b,
                                const Config& cfg);

    // ── Crossover ─────────────────────────────────────────────────────────
    /// Produce offspring. a is assumed to have higher (or equal) fitness.
    static Genome crossover(int offspring_id,
                            const Genome& a,
                            const Genome& b);

    // ── Mutation ──────────────────────────────────────────────────────────
    void mutate_weights(const Config& cfg, std::mt19937_64& rng);
    void mutate_add_connection(const Config& cfg,
                               InnovationTracker& tracker,
                               std::mt19937_64& rng);
    void mutate_add_node(const Config& cfg,
                         InnovationTracker& tracker,
                         std::mt19937_64& rng);
    void mutate_toggle_enable(const Config& cfg, std::mt19937_64& rng);

    // node lookup by id
    const NodeGene* find_node(int id) const;

private:
    int    id_               = 0;
    double fitness_          = 0.0;
    double adjusted_fitness_ = 0.0;

    std::vector<NodeGene>       nodes_;
    std::vector<ConnectionGene> connections_;

    // fast innovation → index lookup (rebuilt on demand)
    mutable std::unordered_map<int,int> innov_to_idx_;
    mutable bool innov_dirty_ = true;

    void rebuild_innov_map() const;
    const std::unordered_map<int,int>& innov_map() const;
};

} // namespace neat
