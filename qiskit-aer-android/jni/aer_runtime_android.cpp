/*
 * Android-compatible wrapper for Qiskit Aer's aer_runtime.cpp.
 *
 * Problem: Android's <sys/types.h> typedef's `uint_t` as `unsigned int`,
 * while Aer's framework defines `AER::uint_t = uint_fast64_t` (unsigned long
 * on 64-bit targets). The aer_runtime.cpp C functions use `uint_t` in their
 * signatures and local variables expecting the Aer type, causing a type
 * mismatch when compiled against the Android NDK sysroot.
 *
 * Fix: Force sys/types.h to be included first, then redefine `uint_t` to
 * `uint_fast64_t` so all subsequent code in aer_runtime.cpp sees the correct
 * type. The macro approach lets us do this without touching upstream source.
 */

// 1. Pull in Android's sys/types.h so it won't be re-included later
#include <sys/types.h>
#include <stdint.h>

// 2. Override the Android uint_t (unsigned int) with the Aer-expected type.
//    The _SYS_INT_TYPES_H guard on some NDK versions uses a typedef – if so,
//    we need both an undef and a #define (macro takes priority over typedef in
//    code that uses the name as a plain token, though not in declarations).
#ifdef uint_t
#undef uint_t
#endif
// Use a macro so every occurrence of `uint_t` in included code resolves to
// uint_fast64_t, matching AER::uint_t.
#define uint_t uint_fast64_t

// 3. Include the Aer runtime – it will see uint_t == uint_fast64_t throughout.
#include "controllers/state_controller.hpp"

// 4. Re-implement the extern "C" API, now with consistent types.
//    (We reproduce the body of aer_runtime.cpp here so we don't need to
//    #include a .cpp file, which is non-standard.)
#include <cmath>
#include <stdio.h>

extern "C" {

void *aer_state() {
    AER::AerState *handler = new AER::AerState();
    return handler;
}

void aer_state_initialize(void *handler) {
    AER::AerState *state = reinterpret_cast<AER::AerState *>(handler);
    state->initialize();
}

void aer_state_finalize(void *handler) {
    AER::AerState *state = reinterpret_cast<AER::AerState *>(handler);
    delete state;
}

void aer_state_configure(void *handler, char *key, char *value) {
    AER::AerState *state = reinterpret_cast<AER::AerState *>(handler);
    state->configure(key, value);
}

uint_t aer_allocate_qubits(void *handler, uint_t num_qubits) {
    AER::AerState *state = reinterpret_cast<AER::AerState *>(handler);
    auto qubit_ids = state->allocate_qubits(num_qubits);
    return qubit_ids[0];
}

uint_t aer_apply_measure(void *handler, uint_t *qubits_, size_t num_qubits) {
    AER::AerState *state = reinterpret_cast<AER::AerState *>(handler);
    std::vector<uint_t> qubits(qubits_, qubits_ + num_qubits);
    return state->apply_measure(qubits);
}

double aer_probability(void *handler, uint_t outcome) {
    AER::AerState *state = reinterpret_cast<AER::AerState *>(handler);
    return state->probability(outcome);
}

// NOTE: aer_amplitude / aer_release_statevector use complex_t (std::complex<double>)
// which has C-linkage issues; they are intentionally omitted here for the
// Android JNI build.  Use the statevector via Java if needed.

void aer_apply_u3(void *handler, uint_t qubit, double theta, double phi, double lambda) {
    AER::AerState *state = reinterpret_cast<AER::AerState *>(handler);
    state->apply_u(qubit, theta, phi, lambda);
}

void aer_apply_p(void *handler, uint_t qubit, double lambda) {
    AER::AerState *state = reinterpret_cast<AER::AerState *>(handler);
    state->apply_mcphase({qubit}, lambda);
}

void aer_apply_x(void *handler, uint_t qubit) {
    AER::AerState *state = reinterpret_cast<AER::AerState *>(handler);
    state->apply_mcx({qubit});
}

void aer_apply_y(void *handler, uint_t qubit) {
    AER::AerState *state = reinterpret_cast<AER::AerState *>(handler);
    state->apply_mcy({qubit});
}

void aer_apply_z(void *handler, uint_t qubit) {
    AER::AerState *state = reinterpret_cast<AER::AerState *>(handler);
    state->apply_mcz({qubit});
}

void aer_apply_h(void *handler, uint_t qubit) {
    AER::AerState *state = reinterpret_cast<AER::AerState *>(handler);
    state->apply_h(qubit);
}

void aer_apply_s(void *handler, uint_t qubit) {
    AER::AerState *state = reinterpret_cast<AER::AerState *>(handler);
    state->apply_u(qubit, 0, 0, M_PI / 2.0);
}

void aer_apply_sdg(void *handler, uint_t qubit) {
    AER::AerState *state = reinterpret_cast<AER::AerState *>(handler);
    state->apply_u(qubit, 0, 0, -M_PI / 2.0);
}

void aer_apply_t(void *handler, uint_t qubit) {
    AER::AerState *state = reinterpret_cast<AER::AerState *>(handler);
    state->apply_u(qubit, 0, 0, M_PI / 4.0);
}

void aer_apply_tdg(void *handler, uint_t qubit) {
    AER::AerState *state = reinterpret_cast<AER::AerState *>(handler);
    state->apply_u(qubit, 0, 0, -M_PI / 4.0);
}

void aer_apply_sx(void *handler, uint_t qubit) {
    AER::AerState *state = reinterpret_cast<AER::AerState *>(handler);
    state->apply_mcsx({qubit});
}

void aer_apply_rx(void *handler, uint_t qubit, double theta) {
    AER::AerState *state = reinterpret_cast<AER::AerState *>(handler);
    state->apply_mcrx({qubit}, theta);
}

void aer_apply_ry(void *handler, uint_t qubit, double theta) {
    AER::AerState *state = reinterpret_cast<AER::AerState *>(handler);
    state->apply_mcry({qubit}, theta);
}

void aer_apply_rz(void *handler, uint_t qubit, double theta) {
    AER::AerState *state = reinterpret_cast<AER::AerState *>(handler);
    state->apply_mcrz({qubit}, theta);
}

void aer_apply_cx(void *handler, uint_t ctrl_qubit, uint_t tgt_qubit) {
    AER::AerState *state = reinterpret_cast<AER::AerState *>(handler);
    state->apply_mcx({ctrl_qubit, tgt_qubit});
}

void aer_apply_cy(void *handler, uint_t ctrl_qubit, uint_t tgt_qubit) {
    AER::AerState *state = reinterpret_cast<AER::AerState *>(handler);
    state->apply_mcy({ctrl_qubit, tgt_qubit});
}

void aer_apply_cz(void *handler, uint_t ctrl_qubit, uint_t tgt_qubit) {
    AER::AerState *state = reinterpret_cast<AER::AerState *>(handler);
    state->apply_mcz({ctrl_qubit, tgt_qubit});
}

void aer_apply_cp(void *handler, uint_t ctrl_qubit, uint_t tgt_qubit, double lambda) {
    AER::AerState *state = reinterpret_cast<AER::AerState *>(handler);
    state->apply_mcphase({ctrl_qubit, tgt_qubit}, lambda);
}

void aer_apply_crx(void *handler, uint_t ctrl_qubit, uint_t tgt_qubit, double theta) {
    AER::AerState *state = reinterpret_cast<AER::AerState *>(handler);
    state->apply_mcrx({ctrl_qubit, tgt_qubit}, theta);
}

void aer_apply_cry(void *handler, uint_t ctrl_qubit, uint_t tgt_qubit, double theta) {
    AER::AerState *state = reinterpret_cast<AER::AerState *>(handler);
    state->apply_mcry({ctrl_qubit, tgt_qubit}, theta);
}

void aer_apply_crz(void *handler, uint_t ctrl_qubit, uint_t tgt_qubit, double theta) {
    AER::AerState *state = reinterpret_cast<AER::AerState *>(handler);
    state->apply_mcrz({ctrl_qubit, tgt_qubit}, theta);
}

void aer_apply_ch(void *handler, uint_t ctrl_qubit, uint_t tgt_qubit) {
    AER::AerState *state = reinterpret_cast<AER::AerState *>(handler);
    state->apply_mcu({ctrl_qubit, tgt_qubit}, M_PI / 2.0, 0, M_PI, 0);
}

void aer_apply_swap(void *handler, uint_t qubit0, uint_t qubit1) {
    AER::AerState *state = reinterpret_cast<AER::AerState *>(handler);
    state->apply_mcswap({qubit0, qubit1});
}

void aer_apply_ccx(void *handler, uint_t qubit0, uint_t qubit1, uint_t qubit2) {
    AER::AerState *state = reinterpret_cast<AER::AerState *>(handler);
    state->apply_mcx({qubit0, qubit1, qubit2});
}

void aer_apply_cswap(void *handler, uint_t ctrl_qubit, uint_t qubit0, uint_t qubit1) {
    AER::AerState *state = reinterpret_cast<AER::AerState *>(handler);
    state->apply_mcswap({ctrl_qubit, qubit0, qubit1});
}

void aer_apply_cu(void *handler, uint_t ctrl_qubit, uint_t tgt_qubit,
                  double theta, double phi, double lambda, double gamma) {
    AER::AerState *state = reinterpret_cast<AER::AerState *>(handler);
    state->apply_mcu({ctrl_qubit, tgt_qubit}, theta, phi, lambda, gamma);
}

} // extern "C"
