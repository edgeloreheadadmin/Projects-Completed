/*
 * neat_jni.cpp — JNI bridge between Android Java and the NEAT C++ library.
 *
 * Java class: com.qiskit.neat.NeatEvolver
 * Library:    libneat.so
 */
#include <jni.h>
#include <android/log.h>
#include <string>
#include <sstream>
#include <stdexcept>

#include "../cpp/neat/neat.hpp"

#define LOG_TAG "NeatEvolver"
#define LOGI(...) __android_log_print(ANDROID_LOG_INFO,  LOG_TAG, __VA_ARGS__)
#define LOGE(...) __android_log_print(ANDROID_LOG_ERROR, LOG_TAG, __VA_ARGS__)

// ── Config from JSON (minimal hand-rolled parser) ─────────────────────────────
// Parses a flat JSON object of key:number pairs.  Full JSON parsing is not
// needed — Android callers use simple config objects.

static double json_double(const std::string& json, const std::string& key,
                          double def = 0.0) {
    std::string search = "\"" + key + "\"";
    auto pos = json.find(search);
    if (pos == std::string::npos) return def;
    pos = json.find(':', pos);
    if (pos == std::string::npos) return def;
    ++pos;
    while (pos < json.size() && (json[pos] == ' ' || json[pos] == '\t')) ++pos;
    try { return std::stod(json.substr(pos)); }
    catch (...) { return def; }
}
static int json_int(const std::string& json, const std::string& key, int def = 0) {
    return static_cast<int>(json_double(json, key, static_cast<double>(def)));
}
static std::string json_string(const std::string& json, const std::string& key,
                               const std::string& def = "") {
    std::string search = "\"" + key + "\"";
    auto pos = json.find(search);
    if (pos == std::string::npos) return def;
    pos = json.find('"', json.find(':', pos) + 1);
    if (pos == std::string::npos) return def;
    ++pos;
    auto end = json.find('"', pos);
    if (end == std::string::npos) return def;
    return json.substr(pos, end - pos);
}

static neat::Config config_from_json(int inputs, int outputs, int pop_size,
                                     const std::string& json) {
    neat::Config cfg;
    cfg.num_inputs       = inputs;
    cfg.num_outputs      = outputs;
    cfg.population_size  = pop_size;

    cfg.compat_threshold = json_double(json, "compat_threshold", cfg.compat_threshold);
    cfg.c1               = json_double(json, "c1",               cfg.c1);
    cfg.c2               = json_double(json, "c2",               cfg.c2);
    cfg.c3               = json_double(json, "c3",               cfg.c3);
    cfg.stagnation_limit = json_int   (json, "stagnation_limit", cfg.stagnation_limit);
    cfg.prob_add_node    = json_double(json, "prob_add_node",    cfg.prob_add_node);
    cfg.prob_add_conn    = json_double(json, "prob_add_conn",    cfg.prob_add_conn);
    cfg.prob_mutate_weights = json_double(json, "prob_mutate_weights", cfg.prob_mutate_weights);
    cfg.prob_perturb     = json_double(json, "prob_perturb",    cfg.prob_perturb);
    cfg.perturb_power    = json_double(json, "perturb_power",   cfg.perturb_power);
    cfg.weight_range     = json_double(json, "weight_range",    cfg.weight_range);
    cfg.prob_crossover   = json_double(json, "prob_crossover",  cfg.prob_crossover);
    cfg.survival_threshold = json_double(json, "survival_threshold", cfg.survival_threshold);
    cfg.elitism          = json_int   (json, "elitism",          cfg.elitism);
    cfg.seed             = static_cast<uint64_t>(json_int(json, "seed", 0));

    std::string act = json_string(json, "activation", "sigmoid");
    cfg.default_activation = neat::activation_from_string(act);
    return cfg;
}

extern "C" {

// ── nativeCreate ──────────────────────────────────────────────────────────────

JNIEXPORT jlong JNICALL
Java_com_qiskit_neat_NeatEvolver_nativeCreate(
        JNIEnv *env, jclass,
        jint inputs, jint outputs, jint pop_size, jstring configJson)
{
    const char *cstr = env->GetStringUTFChars(configJson, nullptr);
    std::string json(cstr);
    env->ReleaseStringUTFChars(configJson, cstr);

    try {
        neat::Config cfg = config_from_json(inputs, outputs, pop_size, json);
        neat::Pool *pool = new neat::Pool(cfg);
        LOGI("Pool created: %d genomes, %d species", pool->population_size(), 0);
        return reinterpret_cast<jlong>(pool);
    } catch (const std::exception& e) {
        LOGE("nativeCreate: %s", e.what());
        return 0L;
    }
}

// ── nativeDestroy ─────────────────────────────────────────────────────────────

JNIEXPORT void JNICALL
Java_com_qiskit_neat_NeatEvolver_nativeDestroy(JNIEnv *, jclass, jlong handle)
{
    delete reinterpret_cast<neat::Pool *>(handle);
}

// ── nativeSetFitness ──────────────────────────────────────────────────────────

JNIEXPORT void JNICALL
Java_com_qiskit_neat_NeatEvolver_nativeSetFitness(
        JNIEnv *, jclass, jlong handle, jint idx, jdouble fitness)
{
    neat::Pool *pool = reinterpret_cast<neat::Pool *>(handle);
    if (idx >= 0 && idx < pool->population_size())
        pool->set_fitness(idx, fitness);
}

// ── nativeNextGeneration ──────────────────────────────────────────────────────

JNIEXPORT void JNICALL
Java_com_qiskit_neat_NeatEvolver_nativeNextGeneration(JNIEnv *, jclass, jlong handle)
{
    reinterpret_cast<neat::Pool *>(handle)->next_generation();
}

// ── nativeActivate ────────────────────────────────────────────────────────────

JNIEXPORT jdoubleArray JNICALL
Java_com_qiskit_neat_NeatEvolver_nativeActivate(
        JNIEnv *env, jclass, jlong handle, jint idx, jdoubleArray inputs)
{
    neat::Pool *pool = reinterpret_cast<neat::Pool *>(handle);
    if (idx < 0 || idx >= pool->population_size()) return nullptr;

    jsize len = env->GetArrayLength(inputs);
    jdouble *in_data = env->GetDoubleArrayElements(inputs, nullptr);
    std::vector<double> in_vec(in_data, in_data + len);
    env->ReleaseDoubleArrayElements(inputs, in_data, JNI_ABORT);

    try {
        neat::Network net = pool->make_network(idx);
        std::vector<double> out = net.activate(in_vec);

        jdoubleArray result = env->NewDoubleArray(static_cast<jsize>(out.size()));
        env->SetDoubleArrayRegion(result, 0, static_cast<jsize>(out.size()), out.data());
        return result;
    } catch (const std::exception& e) {
        LOGE("nativeActivate: %s", e.what());
        return nullptr;
    }
}

// ── nativeGetStats ────────────────────────────────────────────────────────────

JNIEXPORT jstring JNICALL
Java_com_qiskit_neat_NeatEvolver_nativeGetStats(JNIEnv *env, jclass, jlong handle)
{
    neat::Pool *pool = reinterpret_cast<neat::Pool *>(handle);
    neat::PoolStats s = pool->stats();

    std::ostringstream ss;
    ss << "{\"generation\":"    << s.generation
       << ",\"population\":"    << s.population_size
       << ",\"num_species\":"   << s.num_species
       << ",\"best_fitness\":"  << s.best_fitness
       << ",\"avg_fitness\":"   << s.avg_fitness
       << ",\"best_genome_id\":" << s.best_genome_id
       << '}';
    return env->NewStringUTF(ss.str().c_str());
}

// ── nativeGetPopulationSize ───────────────────────────────────────────────────

JNIEXPORT jint JNICALL
Java_com_qiskit_neat_NeatEvolver_nativeGetPopulationSize(JNIEnv *, jclass, jlong handle)
{
    return reinterpret_cast<neat::Pool *>(handle)->population_size();
}

// ── nativeGetBestGenome ───────────────────────────────────────────────────────

JNIEXPORT jstring JNICALL
Java_com_qiskit_neat_NeatEvolver_nativeGetBestGenome(JNIEnv *env, jclass, jlong handle)
{
    std::string json =
        reinterpret_cast<neat::Pool *>(handle)->best_genome_json();
    return env->NewStringUTF(json.c_str());
}

// ── nativeGetVersion ──────────────────────────────────────────────────────────

JNIEXPORT jstring JNICALL
Java_com_qiskit_neat_NeatEvolver_nativeGetVersion(JNIEnv *env, jclass)
{
    return env->NewStringUTF("neat-android-1.0.0-arm64");
}

} // extern "C"
