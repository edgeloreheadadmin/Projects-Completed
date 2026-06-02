/*
 * JNI bridge for Qiskit Aer on Android.
 *
 * Wraps the pure-C API from contrib/runtime/aer_runtime_api.h so that
 * Android Java/Kotlin code can drive the statevector simulator without
 * needing Python.
 *
 * Java package: com.qiskit.aer
 * Class:        AerSimulator
 */
#include <jni.h>
#include <android/log.h>
#include <cstdint>
#include <cstring>

// Forward-declare Aer's C runtime API without including aer_runtime_api.h.
// That header does `typedef uint_fast64_t uint_t;` which conflicts with
// Android's sys/types.h (`typedef unsigned int uint_t;`).
// We use uint_fast64_t directly to match the compiled symbol signatures.
#include <stdint.h>
typedef uint_fast64_t aer_uint_t;
struct complex_t { double real; double imag; };

extern "C" {
    void  *aer_state();
    void   aer_state_initialize(void *h);
    void   aer_state_finalize(void *h);
    void   aer_state_configure(void *h, char *key, char *value);
    aer_uint_t  aer_allocate_qubits(void *h, aer_uint_t n);
    aer_uint_t  aer_apply_measure(void *h, aer_uint_t *qubits, size_t n);
    double aer_probability(void *h, aer_uint_t outcome);
    // Gate functions (qubit indices are aer_uint_t in the compiled object)
    void   aer_apply_h(void *h, aer_uint_t q);
    void   aer_apply_x(void *h, aer_uint_t q);
    void   aer_apply_y(void *h, aer_uint_t q);
    void   aer_apply_z(void *h, aer_uint_t q);
    void   aer_apply_s(void *h, aer_uint_t q);
    void   aer_apply_sdg(void *h, aer_uint_t q);
    void   aer_apply_t(void *h, aer_uint_t q);
    void   aer_apply_tdg(void *h, aer_uint_t q);
    void   aer_apply_sx(void *h, aer_uint_t q);
    void   aer_apply_u3(void *h, aer_uint_t q, double theta, double phi, double lambda);
    void   aer_apply_p(void *h, aer_uint_t q, double lambda);
    void   aer_apply_rx(void *h, aer_uint_t q, double theta);
    void   aer_apply_ry(void *h, aer_uint_t q, double theta);
    void   aer_apply_rz(void *h, aer_uint_t q, double theta);
    void   aer_apply_cx(void *h, aer_uint_t ctrl, aer_uint_t tgt);
    void   aer_apply_cy(void *h, aer_uint_t ctrl, aer_uint_t tgt);
    void   aer_apply_cz(void *h, aer_uint_t ctrl, aer_uint_t tgt);
    void   aer_apply_cp(void *h, aer_uint_t ctrl, aer_uint_t tgt, double lambda);
    void   aer_apply_crx(void *h, aer_uint_t ctrl, aer_uint_t tgt, double theta);
    void   aer_apply_cry(void *h, aer_uint_t ctrl, aer_uint_t tgt, double theta);
    void   aer_apply_crz(void *h, aer_uint_t ctrl, aer_uint_t tgt, double theta);
    void   aer_apply_ch(void *h, aer_uint_t ctrl, aer_uint_t tgt);
    void   aer_apply_swap(void *h, aer_uint_t q0, aer_uint_t q1);
    void   aer_apply_cu(void *h, aer_uint_t ctrl, aer_uint_t tgt,
                        double theta, double phi, double lambda, double gamma);
    void   aer_apply_ccx(void *h, aer_uint_t q0, aer_uint_t q1, aer_uint_t q2);
    void   aer_apply_cswap(void *h, aer_uint_t ctrl, aer_uint_t q0, aer_uint_t q1);
}

#define LOG_TAG "QiskitAer"
#define LOGI(...) __android_log_print(ANDROID_LOG_INFO,  LOG_TAG, __VA_ARGS__)
#define LOGE(...) __android_log_print(ANDROID_LOG_ERROR, LOG_TAG, __VA_ARGS__)

extern "C" {

/* ── State lifecycle ─────────────────────────────────────────────────────── */

JNIEXPORT jlong JNICALL
Java_com_qiskit_aer_AerSimulator_nativeCreateState(JNIEnv *, jclass)
{
    void *state = aer_state();
    LOGI("AerState created: %p", state);
    return reinterpret_cast<jlong>(state);
}

JNIEXPORT void JNICALL
Java_com_qiskit_aer_AerSimulator_nativeInitialize(JNIEnv *, jclass, jlong handle)
{
    aer_state_initialize(reinterpret_cast<void *>(handle));
}

JNIEXPORT void JNICALL
Java_com_qiskit_aer_AerSimulator_nativeFinalize(JNIEnv *, jclass, jlong handle)
{
    aer_state_finalize(reinterpret_cast<void *>(handle));
}

JNIEXPORT void JNICALL
Java_com_qiskit_aer_AerSimulator_nativeConfigure(JNIEnv *env, jclass,
                                                  jlong handle,
                                                  jstring key, jstring value)
{
    const char *k = env->GetStringUTFChars(key,   nullptr);
    const char *v = env->GetStringUTFChars(value, nullptr);
    aer_state_configure(reinterpret_cast<void *>(handle),
                        const_cast<char *>(k),
                        const_cast<char *>(v));
    env->ReleaseStringUTFChars(key,   k);
    env->ReleaseStringUTFChars(value, v);
}

/* ── Qubit allocation ────────────────────────────────────────────────────── */

JNIEXPORT jlong JNICALL
Java_com_qiskit_aer_AerSimulator_nativeAllocateQubits(JNIEnv *, jclass,
                                                        jlong handle,
                                                        jlong numQubits)
{
    return static_cast<jlong>(
        aer_allocate_qubits(reinterpret_cast<void *>(handle),
                            static_cast<aer_uint_t>(numQubits)));
}

/* ── Measurement ─────────────────────────────────────────────────────────── */

JNIEXPORT jlong JNICALL
Java_com_qiskit_aer_AerSimulator_nativeMeasure(JNIEnv *env, jclass,
                                                jlong handle,
                                                jlongArray qubits)
{
    jsize     n     = env->GetArrayLength(qubits);
    jlong    *elems = env->GetLongArrayElements(qubits, nullptr);
    auto     *q     = new aer_uint_t[n];
    for (jsize i = 0; i < n; ++i) q[i] = static_cast<aer_uint_t>(elems[i]);
    env->ReleaseLongArrayElements(qubits, elems, JNI_ABORT);

    aer_uint_t outcome = aer_apply_measure(reinterpret_cast<void *>(handle), q,
                                       static_cast<size_t>(n));
    delete[] q;
    return static_cast<jlong>(outcome);
}

JNIEXPORT jdouble JNICALL
Java_com_qiskit_aer_AerSimulator_nativeProbability(JNIEnv *, jclass,
                                                    jlong handle,
                                                    jlong outcome)
{
    return aer_probability(reinterpret_cast<void *>(handle),
                           static_cast<uint_t>(outcome));
}

/* ── Single-qubit gates ───────────────────────────────────────────────────── */

JNIEXPORT void JNICALL
Java_com_qiskit_aer_AerSimulator_nativeH(JNIEnv *, jclass, jlong h, jlong q)
{ aer_apply_h(reinterpret_cast<void *>(h), q); }

JNIEXPORT void JNICALL
Java_com_qiskit_aer_AerSimulator_nativeX(JNIEnv *, jclass, jlong h, jlong q)
{ aer_apply_x(reinterpret_cast<void *>(h), q); }

JNIEXPORT void JNICALL
Java_com_qiskit_aer_AerSimulator_nativeY(JNIEnv *, jclass, jlong h, jlong q)
{ aer_apply_y(reinterpret_cast<void *>(h), q); }

JNIEXPORT void JNICALL
Java_com_qiskit_aer_AerSimulator_nativeZ(JNIEnv *, jclass, jlong h, jlong q)
{ aer_apply_z(reinterpret_cast<void *>(h), q); }

JNIEXPORT void JNICALL
Java_com_qiskit_aer_AerSimulator_nativeS(JNIEnv *, jclass, jlong h, jlong q)
{ aer_apply_s(reinterpret_cast<void *>(h), q); }

JNIEXPORT void JNICALL
Java_com_qiskit_aer_AerSimulator_nativeSdg(JNIEnv *, jclass, jlong h, jlong q)
{ aer_apply_sdg(reinterpret_cast<void *>(h), q); }

JNIEXPORT void JNICALL
Java_com_qiskit_aer_AerSimulator_nativeT(JNIEnv *, jclass, jlong h, jlong q)
{ aer_apply_t(reinterpret_cast<void *>(h), q); }

JNIEXPORT void JNICALL
Java_com_qiskit_aer_AerSimulator_nativeTdg(JNIEnv *, jclass, jlong h, jlong q)
{ aer_apply_tdg(reinterpret_cast<void *>(h), q); }

JNIEXPORT void JNICALL
Java_com_qiskit_aer_AerSimulator_nativeSX(JNIEnv *, jclass, jlong h, jlong q)
{ aer_apply_sx(reinterpret_cast<void *>(h), q); }

JNIEXPORT void JNICALL
Java_com_qiskit_aer_AerSimulator_nativeU3(JNIEnv *, jclass, jlong h, jlong q,
                                           jdouble theta, jdouble phi, jdouble lambda)
{ aer_apply_u3(reinterpret_cast<void *>(h), q, theta, phi, lambda); }

JNIEXPORT void JNICALL
Java_com_qiskit_aer_AerSimulator_nativeP(JNIEnv *, jclass, jlong h, jlong q, jdouble lambda)
{ aer_apply_p(reinterpret_cast<void *>(h), q, lambda); }

JNIEXPORT void JNICALL
Java_com_qiskit_aer_AerSimulator_nativeRX(JNIEnv *, jclass, jlong h, jlong q, jdouble theta)
{ aer_apply_rx(reinterpret_cast<void *>(h), q, theta); }

JNIEXPORT void JNICALL
Java_com_qiskit_aer_AerSimulator_nativeRY(JNIEnv *, jclass, jlong h, jlong q, jdouble theta)
{ aer_apply_ry(reinterpret_cast<void *>(h), q, theta); }

JNIEXPORT void JNICALL
Java_com_qiskit_aer_AerSimulator_nativeRZ(JNIEnv *, jclass, jlong h, jlong q, jdouble theta)
{ aer_apply_rz(reinterpret_cast<void *>(h), q, theta); }

/* ── Two-qubit gates ─────────────────────────────────────────────────────── */

JNIEXPORT void JNICALL
Java_com_qiskit_aer_AerSimulator_nativeCX(JNIEnv *, jclass, jlong h, jlong ctrl, jlong tgt)
{ aer_apply_cx(reinterpret_cast<void *>(h), ctrl, tgt); }

JNIEXPORT void JNICALL
Java_com_qiskit_aer_AerSimulator_nativeCY(JNIEnv *, jclass, jlong h, jlong ctrl, jlong tgt)
{ aer_apply_cy(reinterpret_cast<void *>(h), ctrl, tgt); }

JNIEXPORT void JNICALL
Java_com_qiskit_aer_AerSimulator_nativeCZ(JNIEnv *, jclass, jlong h, jlong ctrl, jlong tgt)
{ aer_apply_cz(reinterpret_cast<void *>(h), ctrl, tgt); }

JNIEXPORT void JNICALL
Java_com_qiskit_aer_AerSimulator_nativeCP(JNIEnv *, jclass, jlong h, jlong ctrl, jlong tgt,
                                           jdouble lambda)
{ aer_apply_cp(reinterpret_cast<void *>(h), ctrl, tgt, lambda); }

JNIEXPORT void JNICALL
Java_com_qiskit_aer_AerSimulator_nativeCRX(JNIEnv *, jclass, jlong h, jlong ctrl, jlong tgt,
                                            jdouble theta)
{ aer_apply_crx(reinterpret_cast<void *>(h), ctrl, tgt, theta); }

JNIEXPORT void JNICALL
Java_com_qiskit_aer_AerSimulator_nativeCRY(JNIEnv *, jclass, jlong h, jlong ctrl, jlong tgt,
                                            jdouble theta)
{ aer_apply_cry(reinterpret_cast<void *>(h), ctrl, tgt, theta); }

JNIEXPORT void JNICALL
Java_com_qiskit_aer_AerSimulator_nativeCRZ(JNIEnv *, jclass, jlong h, jlong ctrl, jlong tgt,
                                            jdouble theta)
{ aer_apply_crz(reinterpret_cast<void *>(h), ctrl, tgt, theta); }

JNIEXPORT void JNICALL
Java_com_qiskit_aer_AerSimulator_nativeCH(JNIEnv *, jclass, jlong h, jlong ctrl, jlong tgt)
{ aer_apply_ch(reinterpret_cast<void *>(h), ctrl, tgt); }

JNIEXPORT void JNICALL
Java_com_qiskit_aer_AerSimulator_nativeSWAP(JNIEnv *, jclass, jlong h, jlong q0, jlong q1)
{ aer_apply_swap(reinterpret_cast<void *>(h), q0, q1); }

JNIEXPORT void JNICALL
Java_com_qiskit_aer_AerSimulator_nativeCU(JNIEnv *, jclass, jlong h, jlong ctrl, jlong tgt,
                                           jdouble theta, jdouble phi, jdouble lambda,
                                           jdouble gamma)
{ aer_apply_cu(reinterpret_cast<void *>(h), ctrl, tgt, theta, phi, lambda, gamma); }

/* ── Three-qubit gates ───────────────────────────────────────────────────── */

JNIEXPORT void JNICALL
Java_com_qiskit_aer_AerSimulator_nativeCCX(JNIEnv *, jclass, jlong h, jlong q0, jlong q1,
                                            jlong q2)
{ aer_apply_ccx(reinterpret_cast<void *>(h), q0, q1, q2); }

JNIEXPORT void JNICALL
Java_com_qiskit_aer_AerSimulator_nativeCSWAP(JNIEnv *, jclass, jlong h, jlong ctrl,
                                              jlong q0, jlong q1)
{ aer_apply_cswap(reinterpret_cast<void *>(h), ctrl, q0, q1); }

/* ── Version ─────────────────────────────────────────────────────────────── */

JNIEXPORT jstring JNICALL
Java_com_qiskit_aer_AerSimulator_nativeVersion(JNIEnv *env, jclass)
{
    return env->NewStringUTF("qiskit-aer-0.14.2-android-arm64");
}

} // extern "C"
