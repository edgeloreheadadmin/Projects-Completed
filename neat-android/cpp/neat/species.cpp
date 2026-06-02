// species.cpp

#include "species.hpp"
#include <algorithm>
#include <numeric>
#include <random>

namespace neat {

Species::Species(int id, const Genome& representative)
    : id_(id), representative_(std::make_unique<Genome>(representative))
{}

void Species::update_representative() {
    if (members_.empty()) return;
    // Pick a random member as the new representative
    static thread_local std::mt19937 rng(std::random_device{}());
    std::uniform_int_distribution<int> pick(0, static_cast<int>(members_.size()) - 1);
    *representative_ = *members_[pick(rng)];
}

void Species::sort_by_fitness() {
    std::sort(members_.begin(), members_.end(), [](const Genome* a, const Genome* b) {
        return a->fitness() > b->fitness();
    });
}

void Species::compute_shared_fitness() {
    double share = static_cast<double>(members_.size());
    for (auto* g : members_)
        g->set_adjusted_fitness(g->fitness() / share);
}

void Species::update_fitness_stats() {
    if (members_.empty()) return;

    max_fitness_ = 0.0;
    double sum   = 0.0;
    for (const auto* g : members_) {
        if (g->fitness() > max_fitness_) max_fitness_ = g->fitness();
        sum += g->adjusted_fitness();
    }
    avg_fitness_ = sum / members_.size();

    if (max_fitness_ > best_fitness_) {
        best_fitness_ = max_fitness_;
        stagnation_   = 0;
    } else {
        ++stagnation_;
    }
}

} // namespace neat
