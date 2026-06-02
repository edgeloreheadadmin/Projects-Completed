#pragma once
// species.hpp — a group of genomes that share a common ancestor (representative)

#include "genome.hpp"
#include <vector>
#include <memory>

namespace neat {

class Species {
public:
    explicit Species(int id, const Genome& representative);

    int  id()  const { return id_; }
    bool empty() const { return members_.empty(); }
    int  size()  const { return static_cast<int>(members_.size()); }

    // The representative is used for compatibility tests each generation.
    const Genome& representative() const { return *representative_; }

    // Members for the current generation (pointers into the pool's genome list)
    const std::vector<Genome*>& members() const { return members_; }
    std::vector<Genome*>&       members()       { return members_; }

    void clear_members() { members_.clear(); }
    void add_member(Genome* g) { members_.push_back(g); }

    // Update representative to a random member after sorting
    void update_representative();

    // Fitness tracking
    double max_fitness()    const { return max_fitness_; }
    double avg_fitness()    const { return avg_fitness_; }
    int    stagnation()     const { return stagnation_; }

    void compute_shared_fitness();   // divides each member's fitness by species size
    void update_fitness_stats();     // call after compute_shared_fitness

    bool is_stagnant(int limit) const { return stagnation_ >= limit; }

    // Sort members by (raw) fitness descending
    void sort_by_fitness();

private:
    int               id_;
    std::unique_ptr<Genome> representative_;

    std::vector<Genome*> members_;

    double max_fitness_  = 0.0;
    double avg_fitness_  = 0.0;
    double best_fitness_ = 0.0;  // historical best for stagnation check
    int    stagnation_   = 0;
};

} // namespace neat
