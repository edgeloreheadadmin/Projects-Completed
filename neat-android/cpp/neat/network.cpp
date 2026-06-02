// network.cpp — feed-forward network evaluation

#include "network.hpp"
#include <cmath>
#include <algorithm>
#include <stdexcept>
#include <unordered_map>
#include <unordered_set>

namespace neat {

// ── Topological sort (Kahn's algorithm) ───────────────────────────────────────

Network::Network(const Genome& g, double bias_value)
    : bias_value_(bias_value)
{
    // Build adjacency + in-degree for Kahn's
    const auto& nodes = g.nodes();
    const auto& conns = g.connections();

    // Map node_id → local index
    for (int i = 0; i < static_cast<int>(nodes.size()); ++i)
        id_to_idx_[nodes[i].id] = i;

    // in-degree (counting only enabled connections)
    std::unordered_map<int,int> in_degree;
    for (const auto& n : nodes) in_degree[n.id] = 0;
    std::unordered_map<int, std::vector<std::pair<int,double>>> adj; // id → [(to_id, weight)]
    for (const auto& c : conns) {
        if (!c.enabled) continue;
        adj[c.in_node].push_back({c.out_node, c.weight});
        ++in_degree[c.out_node];
    }

    // Seed queue with INPUT and BIAS nodes (no predecessors)
    std::vector<int> queue;
    for (const auto& n : nodes)
        if (in_degree[n.id] == 0) queue.push_back(n.id);

    std::vector<int> topo_order;
    while (!queue.empty()) {
        int cur = queue.back(); queue.pop_back();
        topo_order.push_back(cur);
        for (auto& [nxt, w] : adj[cur]) {
            if (--in_degree[nxt] == 0)
                queue.push_back(nxt);
        }
    }
    // If there are cycles (recurrent), add remaining nodes at the end
    for (const auto& n : nodes) {
        bool found = false;
        for (int id : topo_order) if (id == n.id) { found = true; break; }
        if (!found) topo_order.push_back(n.id);
    }

    // Build neurons in topo order
    for (int node_id : topo_order) {
        const NodeGene* ng = g.find_node(node_id);
        if (!ng) continue;
        Neuron nu;
        nu.node_id   = node_id;
        nu.type      = ng->type;
        nu.activation = ng->activation;
        nu.value     = 0.0;
        neurons_.push_back(nu);
        if (ng->type == NodeType::INPUT)  ++num_inputs_;
        if (ng->type == NodeType::OUTPUT) ++num_outputs_;
    }

    // Rebuild id_to_idx_ using the new neuron order
    id_to_idx_.clear();
    for (int i = 0; i < static_cast<int>(neurons_.size()); ++i)
        id_to_idx_[neurons_[i].node_id] = i;

    // Collect enabled edges (in topo order)
    for (const auto& c : conns) {
        if (!c.enabled) continue;
        edges_.push_back({c.in_node, c.out_node, c.weight});
    }
}

// ── activate ──────────────────────────────────────────────────────────────────

std::vector<double> Network::activate(const std::vector<double>& inputs)
{
    // Reset all neuron values
    for (auto& n : neurons_) n.value = 0.0;

    // Load inputs and bias
    int input_idx = 0;
    for (auto& n : neurons_) {
        if (n.type == NodeType::INPUT) {
            if (input_idx < static_cast<int>(inputs.size()))
                n.value = inputs[input_idx++];
        } else if (n.type == NodeType::BIAS) {
            n.value = bias_value_;
        }
    }

    // Propagate through edges (already in topo order)
    for (const auto& e : edges_) {
        auto it_from = id_to_idx_.find(e.from);
        auto it_to   = id_to_idx_.find(e.to);
        if (it_from == id_to_idx_.end() || it_to == id_to_idx_.end()) continue;
        neurons_[it_to->second].value +=
            neurons_[it_from->second].value * e.weight;
    }

    // Apply activation to non-input, non-bias neurons
    for (auto& n : neurons_) {
        if (n.type == NodeType::INPUT || n.type == NodeType::BIAS) continue;
        n.value = apply_activation(n.value, n.activation);
    }

    // Collect outputs
    std::vector<double> out;
    for (const auto& n : neurons_)
        if (n.type == NodeType::OUTPUT)
            out.push_back(n.value);
    return out;
}

// ── apply_activation ──────────────────────────────────────────────────────────

double Network::apply_activation(double x, Activation a) {
    switch (a) {
        case Activation::TANH:    return std::tanh(x);
        case Activation::RELU:    return x > 0.0 ? x : 0.0;
        case Activation::LINEAR:  return x;
        default: /* SIGMOID */    return 1.0 / (1.0 + std::exp(-4.9 * x));
    }
}

} // namespace neat
