#pragma once
// network.hpp — phenotype: build a feed-forward net from a Genome and evaluate it

#include "genome.hpp"
#include <vector>
#include <unordered_map>

namespace neat {

/// A single neuron in the evaluated network (activation value + metadata).
struct Neuron {
    int        node_id;
    NodeType   type;
    Activation activation;
    double     value = 0.0;  // current activation
};

/// Feed-forward neural network built from a Genome.
/// The network performs a topological sort at construction time
/// so activation is O(connections) per call.
class Network {
public:
    explicit Network(const Genome& g, double bias_value = 1.0);

    /// Activate with inputs; returns output values (one per OUTPUT node).
    /// inputs.size() must equal num_inputs.
    std::vector<double> activate(const std::vector<double>& inputs);

    int num_inputs()  const { return num_inputs_; }
    int num_outputs() const { return num_outputs_; }

private:
    struct Edge { int from; int to; double weight; };

    std::vector<Neuron> neurons_;        // in topological order
    std::vector<Edge>   edges_;
    std::unordered_map<int,int> id_to_idx_;

    int num_inputs_  = 0;
    int num_outputs_ = 0;
    double bias_value_ = 1.0;

    static double apply_activation(double x, Activation a);
};

} // namespace neat
