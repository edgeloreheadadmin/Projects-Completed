// genome.cpp — implementation of InnovationTracker and Genome

#include "genome.hpp"
#include <algorithm>
#include <cmath>
#include <random>
#include <stdexcept>
#include <unordered_set>

namespace neat {

// ── InnovationTracker ────────────────────────────────────────────────────────

int InnovationTracker::get_or_create_connection(int in_node, int out_node) {
    uint64_t k = key(in_node, out_node);
    auto it = gen_map_.find(k);
    if (it != gen_map_.end()) return it->second;
    int id = next_innovation_++;
    gen_map_[k] = id;
    return id;
}

int InnovationTracker::new_node_id() {
    return next_node_id_++;
}

void InnovationTracker::new_generation() {
    gen_map_.clear();
}

// ── Genome helpers ────────────────────────────────────────────────────────────

void Genome::add_node(const NodeGene& ng) {
    nodes_.push_back(ng);
    innov_dirty_ = true;
}

void Genome::add_connection(const ConnectionGene& cg) {
    connections_.push_back(cg);
    innov_dirty_ = true;
}

const NodeGene* Genome::find_node(int id) const {
    for (const auto& n : nodes_)
        if (n.id == id) return &n;
    return nullptr;
}

void Genome::rebuild_innov_map() const {
    innov_to_idx_.clear();
    for (int i = 0; i < static_cast<int>(connections_.size()); ++i)
        innov_to_idx_[connections_[i].innovation_id] = i;
    innov_dirty_ = false;
}

const std::unordered_map<int,int>& Genome::innov_map() const {
    if (innov_dirty_) rebuild_innov_map();
    return innov_to_idx_;
}

// ── create_minimal ────────────────────────────────────────────────────────────

Genome Genome::create_minimal(int genome_id,
                               int num_inputs,
                               int num_outputs,
                               bool add_bias,
                               const Config& cfg,
                               InnovationTracker& tracker)
{
    Genome g(genome_id);

    // Input nodes
    std::vector<int> input_ids;
    for (int i = 0; i < num_inputs; ++i) {
        int nid = tracker.new_node_id();
        g.add_node(NodeGene(nid, NodeType::INPUT, Activation::LINEAR));
        input_ids.push_back(nid);
    }

    // Optional bias node
    int bias_id = -1;
    if (add_bias) {
        bias_id = tracker.new_node_id();
        g.add_node(NodeGene(bias_id, NodeType::BIAS, Activation::LINEAR));
    }

    // Output nodes
    std::vector<int> output_ids;
    for (int i = 0; i < num_outputs; ++i) {
        int nid = tracker.new_node_id();
        g.add_node(NodeGene(nid, NodeType::OUTPUT, cfg.default_activation));
        output_ids.push_back(nid);
    }

    // Connect inputs → outputs (fully connected initial topology)
    std::mt19937_64 rng(cfg.seed ^ static_cast<uint64_t>(genome_id));
    std::uniform_real_distribution<double> wdist(-cfg.weight_range, cfg.weight_range);

    for (int in_id : input_ids) {
        for (int out_id : output_ids) {
            int innov = tracker.get_or_create_connection(in_id, out_id);
            g.add_connection(ConnectionGene(in_id, out_id, wdist(rng), true, innov));
        }
    }
    if (add_bias && bias_id >= 0) {
        for (int out_id : output_ids) {
            int innov = tracker.get_or_create_connection(bias_id, out_id);
            g.add_connection(ConnectionGene(bias_id, out_id, wdist(rng), true, innov));
        }
    }

    return g;
}

// ── compatibility distance ────────────────────────────────────────────────────

double Genome::compatibility(const Genome& a, const Genome& b, const Config& cfg) {
    const auto& map_a = a.innov_map();
    const auto& map_b = b.innov_map();

    int max_innov_a = 0, max_innov_b = 0;
    for (const auto& c : a.connections_) max_innov_a = std::max(max_innov_a, c.innovation_id);
    for (const auto& c : b.connections_) max_innov_b = std::max(max_innov_b, c.innovation_id);
    int max_innov = std::max(max_innov_a, max_innov_b);

    int excess   = 0;
    int disjoint = 0;
    double weight_diff = 0.0;
    int matching = 0;

    // Walk all innovation ids up to max
    for (const auto& [innov, idx_a] : map_a) {
        auto it_b = map_b.find(innov);
        if (it_b != map_b.end()) {
            // matching gene
            weight_diff += std::fabs(a.connections_[idx_a].weight
                                   - b.connections_[it_b->second].weight);
            ++matching;
        } else {
            // gene is in a but not b
            if (innov > max_innov_b) ++excess;
            else                     ++disjoint;
        }
    }
    for (const auto& [innov, idx_b] : map_b) {
        if (map_a.find(innov) == map_a.end()) {
            if (innov > max_innov_a) ++excess;
            else                     ++disjoint;
        }
    }

    int N = std::max(1, static_cast<int>(std::max(a.connections_.size(),
                                                   b.connections_.size())));
    // NEAT paper: N=1 if both genomes are small (< 20 genes)
    if (N < 20) N = 1;

    double W = (matching > 0) ? weight_diff / matching : 0.0;
    return (cfg.c1 * excess + cfg.c2 * disjoint) / N + cfg.c3 * W;
}

// ── crossover ─────────────────────────────────────────────────────────────────

Genome Genome::crossover(int offspring_id, const Genome& a, const Genome& b) {
    // a is the fitter (or equal) parent
    // Matching genes: inherit randomly; disjoint/excess from a (fitter parent)
    Genome offspring(offspring_id);

    // Inherit all nodes from fitter parent (a), plus any extra from b
    for (const auto& n : a.nodes_)
        offspring.add_node(n);
    // add hidden nodes from b that aren't in a
    for (const auto& n : b.nodes_) {
        if (n.type == NodeType::HIDDEN && offspring.find_node(n.id) == nullptr)
            offspring.add_node(n);
    }

    // Align connections by innovation
    const auto& map_b = b.innov_map();
    static thread_local std::mt19937_64 rng(std::random_device{}());
    std::uniform_int_distribution<int> coin(0, 1);

    for (const auto& cg_a : a.connections_) {
        auto it_b = map_b.find(cg_a.innovation_id);
        if (it_b != map_b.end()) {
            // Matching: pick randomly
            const auto& cg_b = b.connections_[it_b->second];
            ConnectionGene chosen = (coin(rng) == 0) ? cg_a : cg_b;
            // If either parent has it disabled, child has 75% chance disabled
            if (!cg_a.enabled || !cg_b.enabled) {
                std::uniform_real_distribution<double> ud(0.0, 1.0);
                chosen.enabled = (ud(rng) > 0.75);
            }
            offspring.add_connection(chosen);
        } else {
            // Disjoint / excess from a: always inherit
            offspring.add_connection(cg_a);
        }
    }

    return offspring;
}

// ── mutate_weights ────────────────────────────────────────────────────────────

void Genome::mutate_weights(const Config& cfg, std::mt19937_64& rng) {
    std::uniform_real_distribution<double> ud(0.0, 1.0);
    std::normal_distribution<double>       nd(0.0, cfg.perturb_power);
    std::uniform_real_distribution<double> wr(-cfg.weight_range, cfg.weight_range);

    for (auto& cg : connections_) {
        if (ud(rng) < cfg.prob_mutate_weights) {
            if (ud(rng) < cfg.prob_perturb)
                cg.weight += nd(rng);
            else
                cg.weight = wr(rng);
            // Clamp
            if (cg.weight >  cfg.weight_range) cg.weight =  cfg.weight_range;
            if (cg.weight < -cfg.weight_range) cg.weight = -cfg.weight_range;
        }
    }
}

// ── mutate_add_connection ─────────────────────────────────────────────────────

void Genome::mutate_add_connection(const Config& cfg,
                                   InnovationTracker& tracker,
                                   std::mt19937_64& rng)
{
    // Collect candidate (from, to) pairs; to must not be INPUT or BIAS
    // and no recurrent connections (feed-forward only): out_node layer > in_node layer
    // We allow any → output or hidden with no existing connection.

    // Build existing connection set
    std::unordered_set<uint64_t> existing;
    auto conn_key = [](int a, int b) -> uint64_t {
        return (static_cast<uint64_t>(a) << 32) | static_cast<uint32_t>(b);
    };
    for (const auto& cg : connections_)
        existing.insert(conn_key(cg.in_node, cg.out_node));

    // Potential sources: INPUT, BIAS, HIDDEN
    // Potential targets: HIDDEN, OUTPUT
    std::vector<int> sources, targets;
    for (const auto& n : nodes_) {
        if (n.type != NodeType::OUTPUT) sources.push_back(n.id);
        if (n.type != NodeType::INPUT && n.type != NodeType::BIAS)
            targets.push_back(n.id);
    }

    // Shuffle and try up to 20 random pairs
    std::shuffle(sources.begin(), sources.end(), rng);
    std::shuffle(targets.begin(), targets.end(), rng);

    for (int s : sources) {
        for (int t : targets) {
            if (s == t) continue;
            if (existing.count(conn_key(s, t))) continue;
            // Add this connection
            int innov = tracker.get_or_create_connection(s, t);
            std::uniform_real_distribution<double> wr(-cfg.weight_range, cfg.weight_range);
            add_connection(ConnectionGene(s, t, wr(rng), true, innov));
            return;
        }
    }
}

// ── mutate_add_node ───────────────────────────────────────────────────────────

void Genome::mutate_add_node(const Config& cfg,
                              InnovationTracker& tracker,
                              std::mt19937_64& rng)
{
    // Pick a random enabled connection to split
    std::vector<int> enabled_idx;
    for (int i = 0; i < static_cast<int>(connections_.size()); ++i)
        if (connections_[i].enabled) enabled_idx.push_back(i);
    if (enabled_idx.empty()) return;

    std::uniform_int_distribution<int> pick(0, static_cast<int>(enabled_idx.size()) - 1);
    int idx = enabled_idx[pick(rng)];
    auto& old_conn = connections_[idx];

    // Disable the old connection
    old_conn.enabled = false;

    // Create a new hidden node
    int new_nid = tracker.new_node_id();
    add_node(NodeGene(new_nid, NodeType::HIDDEN, cfg.default_activation));

    // in → new_node (weight 1.0)
    int innov1 = tracker.get_or_create_connection(old_conn.in_node, new_nid);
    add_connection(ConnectionGene(old_conn.in_node, new_nid, 1.0, true, innov1));

    // new_node → out (weight = old weight)
    int innov2 = tracker.get_or_create_connection(new_nid, old_conn.out_node);
    add_connection(ConnectionGene(new_nid, old_conn.out_node, old_conn.weight, true, innov2));

    innov_dirty_ = true;
}

// ── mutate_toggle_enable ──────────────────────────────────────────────────────

void Genome::mutate_toggle_enable(const Config& cfg, std::mt19937_64& rng) {
    if (connections_.empty()) return;
    std::uniform_int_distribution<int> pick(0, static_cast<int>(connections_.size()) - 1);
    int idx = pick(rng);
    connections_[idx].enabled = !connections_[idx].enabled;
}

} // namespace neat
