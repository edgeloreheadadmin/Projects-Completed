// pool.cpp — population manager implementation

#include "pool.hpp"
#include <algorithm>
#include <numeric>
#include <sstream>
#include <cmath>
#include <cassert>

namespace neat {

// ── Constructor ───────────────────────────────────────────────────────────────

Pool::Pool(const Config& cfg) : cfg_(cfg)
{
    uint64_t seed = cfg_.seed ? cfg_.seed
                              : static_cast<uint64_t>(std::random_device{}());
    rng_.seed(seed);

    // Create initial minimal genomes
    genomes_.reserve(cfg_.population_size);
    for (int i = 0; i < cfg_.population_size; ++i) {
        genomes_.push_back(Genome::create_minimal(
            new_genome_id(), cfg_.num_inputs, cfg_.num_outputs,
            /*add_bias=*/true, cfg_, tracker_));
    }
    tracker_.new_generation();

    // Mutate weights to break symmetry
    for (auto& g : genomes_)
        g.mutate_weights(cfg_, rng_);

    speciate();
}

// ── make_network ──────────────────────────────────────────────────────────────

Network Pool::make_network(int idx) const {
    return Network(genomes_[idx], cfg_.bias_value);
}

// ── next_generation ───────────────────────────────────────────────────────────

void Pool::next_generation() {
    compute_fitness();
    reproduce();
    ++generation_;
    tracker_.new_generation();
}

// ── speciate ──────────────────────────────────────────────────────────────────

void Pool::speciate() {
    // Clear current membership (keep species objects and representatives)
    for (auto& sp : species_) sp->clear_members();

    for (auto& g : genomes_) {
        bool placed = false;
        for (auto& sp : species_) {
            double d = Genome::compatibility(g, sp->representative(), cfg_);
            if (d < cfg_.compat_threshold) {
                sp->add_member(&g);
                placed = true;
                break;
            }
        }
        if (!placed) {
            auto sp = std::make_unique<Species>(new_species_id(), g);
            sp->add_member(&g);
            species_.push_back(std::move(sp));
        }
    }

    // Remove empty species
    species_.erase(
        std::remove_if(species_.begin(), species_.end(),
                       [](const std::unique_ptr<Species>& s){ return s->empty(); }),
        species_.end());

    // Update representatives for next round
    for (auto& sp : species_) sp->update_representative();
}

// ── compute_fitness ───────────────────────────────────────────────────────────

void Pool::compute_fitness() {
    // Fitness sharing + stagnation tracking per species
    for (auto& sp : species_) {
        sp->sort_by_fitness();
        sp->compute_shared_fitness();
        sp->update_fitness_stats();
    }

    // Find global best
    global_best_fitness_ = 0.0;
    for (int i = 0; i < static_cast<int>(genomes_.size()); ++i) {
        if (genomes_[i].fitness() > global_best_fitness_) {
            global_best_fitness_ = genomes_[i].fitness();
            best_genome_idx_     = i;
        }
    }
}

// ── reproduce ────────────────────────────────────────────────────────────────

void Pool::reproduce() {
    // Cull stagnant species (keep at least 2 if there are multiple)
    if (species_.size() > 2) {
        species_.erase(
            std::remove_if(species_.begin(), species_.end(),
                [this](const std::unique_ptr<Species>& sp) {
                    return sp->is_stagnant(cfg_.stagnation_limit);
                }),
            species_.end());
    }

    if (species_.empty()) {
        // Shouldn't normally happen; reset
        genomes_.clear();
        for (int i = 0; i < cfg_.population_size; ++i)
            genomes_.push_back(Genome::create_minimal(
                new_genome_id(), cfg_.num_inputs, cfg_.num_outputs,
                true, cfg_, tracker_));
        speciate();
        return;
    }

    // Compute total adjusted fitness
    double total_adj = 0.0;
    for (const auto& sp : species_)
        for (const auto* g : sp->members())
            total_adj += g->adjusted_fitness();
    if (total_adj <= 0.0) total_adj = 1.0;

    // Offspring count per species (proportional to sum of adjusted fitness)
    std::vector<int> offspring_counts(species_.size(), 0);
    int allocated = 0;
    for (int i = 0; i < static_cast<int>(species_.size()); ++i) {
        double sp_adj = 0.0;
        for (const auto* g : species_[i]->members())
            sp_adj += g->adjusted_fitness();
        int count = static_cast<int>(std::round(sp_adj / total_adj * cfg_.population_size));
        offspring_counts[i] = std::max(count, 1);
        allocated += offspring_counts[i];
    }
    // Adjust rounding to hit exact population_size
    int diff = cfg_.population_size - allocated;
    if (diff > 0) offspring_counts[0] += diff;
    else while (diff < 0) {
        for (int i = static_cast<int>(species_.size()) - 1; i >= 0 && diff < 0; --i) {
            if (offspring_counts[i] > 1) { --offspring_counts[i]; ++diff; }
        }
    }

    // Save best genome from previous generation
    Genome prev_best = genomes_[best_genome_idx_];

    // Build next generation
    std::vector<Genome> next_gen;
    next_gen.reserve(cfg_.population_size);

    // Elites: copy top genome of each species (up to elitism limit)
    for (auto& sp : species_) {
        sp->sort_by_fitness();
        for (int e = 0; e < cfg_.elitism && e < sp->size(); ++e) {
            Genome elite = *sp->members()[e];
            elite.set_id(new_genome_id());
            next_gen.push_back(std::move(elite));
        }
    }

    // Breed remaining offspring
    int bred = 0;
    for (int si = 0; si < static_cast<int>(species_.size()); ++si) {
        auto& sp = species_[si];
        int to_breed = offspring_counts[si] - cfg_.elitism;
        // Cull to survival_threshold
        int survivors = std::max(1, static_cast<int>(
            std::ceil(sp->size() * cfg_.survival_threshold)));
        while (sp->size() > survivors)
            sp->members().pop_back();  // already sorted descending

        for (int b = 0; b < to_breed && bred < cfg_.population_size; ++b, ++bred) {
            next_gen.push_back(breed_offspring(new_genome_id()));
            // If we went over on elites, trim
            if (static_cast<int>(next_gen.size()) >= cfg_.population_size) goto done;
        }
    }
    done:
    // Pad if short (shouldn't normally happen)
    while (static_cast<int>(next_gen.size()) < cfg_.population_size) {
        Genome child = prev_best;
        child.set_id(new_genome_id());
        child.mutate_weights(cfg_, rng_);
        next_gen.push_back(std::move(child));
    }
    // Trim if over
    while (static_cast<int>(next_gen.size()) > cfg_.population_size)
        next_gen.pop_back();

    genomes_ = std::move(next_gen);
    speciate();
}

// ── breed_offspring ───────────────────────────────────────────────────────────

Genome Pool::breed_offspring(int new_id) {
    std::uniform_real_distribution<double> ud(0.0, 1.0);

    // Pick a species at random, weighted by avg adjusted fitness
    double total = 0.0;
    for (const auto& sp : species_) total += sp->avg_fitness();
    if (total <= 0.0) total = 1.0;

    Species* chosen_sp = nullptr;
    double r = ud(rng_) * total;
    for (auto& sp : species_) {
        r -= sp->avg_fitness();
        if (r <= 0.0) { chosen_sp = sp.get(); break; }
    }
    if (!chosen_sp) chosen_sp = species_.front().get();

    Genome* parent_a = chosen_sp->members()[0]; // best in species

    if (ud(rng_) < cfg_.prob_crossover) {
        // Choose second parent
        Genome* parent_b = nullptr;

        if (ud(rng_) < cfg_.prob_interspecies && species_.size() > 1) {
            // Interspecies: pick random from another species
            std::uniform_int_distribution<int> sp_pick(
                0, static_cast<int>(species_.size()) - 1);
            Species* other = nullptr;
            for (int attempt = 0; attempt < 10; ++attempt) {
                other = species_[sp_pick(rng_)].get();
                if (other != chosen_sp && !other->members().empty()) break;
            }
            if (other && !other->members().empty()) {
                std::uniform_int_distribution<int> pick(
                    0, static_cast<int>(other->members().size()) - 1);
                parent_b = other->members()[pick(rng_)];
            }
        }
        if (!parent_b && chosen_sp->size() > 1) {
            std::uniform_int_distribution<int> pick(
                0, static_cast<int>(chosen_sp->members().size()) - 1);
            parent_b = chosen_sp->members()[pick(rng_)];
        }

        if (parent_b) {
            const Genome* fitter = (parent_a->fitness() >= parent_b->fitness())
                                    ? parent_a : parent_b;
            const Genome* weaker = (fitter == parent_a) ? parent_b : parent_a;
            Genome child = Genome::crossover(new_id, *fitter, *weaker);
            child.mutate_weights(cfg_, rng_);
            if (ud(rng_) < cfg_.prob_add_node)
                child.mutate_add_node(cfg_, tracker_, rng_);
            if (ud(rng_) < cfg_.prob_add_conn)
                child.mutate_add_connection(cfg_, tracker_, rng_);
            if (ud(rng_) < cfg_.prob_toggle_enable)
                child.mutate_toggle_enable(cfg_, rng_);
            return child;
        }
    }

    // Asexual reproduction: clone + mutate
    Genome child = *parent_a;
    child.set_id(new_id);
    child.mutate_weights(cfg_, rng_);
    if (ud(rng_) < cfg_.prob_add_node)
        child.mutate_add_node(cfg_, tracker_, rng_);
    if (ud(rng_) < cfg_.prob_add_conn)
        child.mutate_add_connection(cfg_, tracker_, rng_);
    if (ud(rng_) < cfg_.prob_toggle_enable)
        child.mutate_toggle_enable(cfg_, rng_);
    return child;
}

// ── stats ─────────────────────────────────────────────────────────────────────

PoolStats Pool::stats() const {
    PoolStats s;
    s.generation      = generation_;
    s.population_size = static_cast<int>(genomes_.size());
    s.num_species     = static_cast<int>(species_.size());
    s.best_fitness    = global_best_fitness_;
    s.best_genome_id  = genomes_.empty() ? -1 : genomes_[best_genome_idx_].id();

    double sum = 0.0;
    for (const auto& g : genomes_) sum += g.fitness();
    s.avg_fitness = genomes_.empty() ? 0.0 : sum / genomes_.size();
    return s;
}

// ── best_genome_json ──────────────────────────────────────────────────────────

std::string Pool::best_genome_json() const {
    if (genomes_.empty()) return "{}";
    const Genome& g = genomes_[best_genome_idx_];

    std::ostringstream ss;
    ss << "{\"id\":" << g.id()
       << ",\"fitness\":" << g.fitness()
       << ",\"nodes\":[";
    for (int i = 0; i < static_cast<int>(g.nodes().size()); ++i) {
        const auto& n = g.nodes()[i];
        if (i) ss << ',';
        ss << "{\"id\":" << n.id
           << ",\"type\":" << static_cast<int>(n.type)
           << ",\"activation\":\"" << activation_to_string(n.activation) << "\"}";
    }
    ss << "],\"connections\":[";
    for (int i = 0; i < static_cast<int>(g.connections().size()); ++i) {
        const auto& c = g.connections()[i];
        if (i) ss << ',';
        ss << "{\"in\":" << c.in_node
           << ",\"out\":" << c.out_node
           << ",\"weight\":" << c.weight
           << ",\"enabled\":" << (c.enabled ? "true" : "false")
           << ",\"innov\":" << c.innovation_id << '}';
    }
    ss << "]}";
    return ss.str();
}

} // namespace neat
