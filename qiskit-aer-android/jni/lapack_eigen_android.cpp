/*
 * Android-specific LAPACK shims using Eigen C++.
 *
 * Provides functions not covered by Eigen's own LAPACK back-end:
 *   - zheevx_  (complex Hermitian eigenvalue expert driver)
 *   - cheevx_  (complex float variant, for completeness)
 *   - dsyevx_  (real symmetric expert driver)
 *
 * These are implemented by delegating to Eigen's SelfAdjointEigenSolver,
 * which is identical in results for positive-definite and general Hermitian
 * problems.  The "expert" options (range selection, tolerances) are partially
 * supported: RANGE='A' (all eigenvalues) is fully supported; RANGE='I' and
 * RANGE='V' fall back to computing all then selecting.
 */

#include <sys/types.h>
#ifdef uint_t
#undef uint_t
#endif
#define uint_t uint_fast64_t

#include <Eigen/Eigenvalues>
#include <complex>
#include <cstring>
#include <algorithm>
#include <vector>

// ── Fortran calling convention: characters passed by pointer ──────────────

extern "C" {

// ─── zheevx: complex double Hermitian eigenvalue expert driver ─────────────
//
// jobz  'N'=eigenvalues only, 'V'=eigenvalues+vectors
// range 'A'=all, 'I'=index range il..iu, 'V'=value range vl..vu
// uplo  'U' or 'L' (ignored here – Eigen always uses full matrix)
// n     matrix size
// a     n×n matrix (in/out: overwritten by eigenvectors if jobz='V')
// lda   leading dimension of a
// vl,vu value range bounds (used only when range='V')
// il,iu 1-based index range (used only when range='I')
// abstol convergence tolerance (ignored – Eigen determines internally)
// m     (out) number of eigenvalues found
// w     (out) eigenvalues (up to n)
// z     (out) eigenvectors (m columns), size ldz×m
// ldz   leading dim of z
// work, lwork, rwork, iwork, ifail: workspace (ignored)
// info  (out) 0 = success

int zheevx_(char *jobz, char *range, char *uplo,
            int *n, std::complex<double> *a, int *lda,
            double *vl, double *vu, int *il, int *iu, double *abstol,
            int *m, double *w,
            std::complex<double> *z, int *ldz,
            std::complex<double> *work, int *lwork,
            double *rwork, int *iwork, int *ifail,
            int *info)
{
    (void)uplo; (void)abstol; (void)work; (void)lwork;
    (void)rwork; (void)iwork; (void)ifail;

    *info = 0;
    int N = *n;

    // Workspace query
    if (*lwork == -1) {
        if (work) work[0] = std::complex<double>(64 * N, 0);
        return 0;
    }

    using MatC = Eigen::Matrix<std::complex<double>, Eigen::Dynamic, Eigen::Dynamic,
                               Eigen::ColMajor>;

    Eigen::Map<MatC> A(a, N, N);
    Eigen::SelfAdjointEigenSolver<MatC> solver(A);
    if (solver.info() != Eigen::Success) { *info = -1; return 0; }

    const auto &vals = solver.eigenvalues();   // ascending order
    const auto &vecs = solver.eigenvectors();

    // Determine which eigenvalues to return
    int lo = 0, hi = N;    // [lo, hi) in 0-based
    if (*range == 'I' || *range == 'i') {
        lo = *il - 1;      // convert 1-based to 0-based
        hi = *iu;
    } else if (*range == 'V' || *range == 'v') {
        lo = 0; hi = 0;
        for (int k = 0; k < N; ++k) {
            if (vals(k) <  *vl)  lo = k + 1;
            if (vals(k) <= *vu)  hi = k + 1;
        }
    }
    *m = hi - lo;

    // Copy eigenvalues
    for (int k = 0; k < *m; ++k)
        w[k] = vals(lo + k);

    // Copy eigenvectors if requested
    if (*jobz == 'V' || *jobz == 'v') {
        Eigen::Map<MatC> Z(z, N, *m);
        Z = vecs.block(0, lo, N, *m);
    }

    return 0;
}

// ─── cheevx: complex float Hermitian eigenvalue expert driver ──────────────
int cheevx_(char *jobz, char *range, char *uplo,
            int *n, std::complex<float> *a, int *lda,
            float *vl, float *vu, int *il, int *iu, float *abstol,
            int *m, float *w,
            std::complex<float> *z, int *ldz,
            std::complex<float> *work, int *lwork,
            float *rwork, int *iwork, int *ifail,
            int *info)
{
    (void)uplo; (void)abstol; (void)work; (void)lwork;
    (void)rwork; (void)iwork; (void)ifail;

    *info = 0;
    int N = *n;
    if (*lwork == -1) {
        if (work) work[0] = std::complex<float>(64.f * N, 0);
        return 0;
    }

    using MatC = Eigen::Matrix<std::complex<float>, Eigen::Dynamic, Eigen::Dynamic,
                               Eigen::ColMajor>;
    Eigen::Map<MatC> A(a, N, N);
    Eigen::SelfAdjointEigenSolver<MatC> solver(A);
    if (solver.info() != Eigen::Success) { *info = -1; return 0; }

    const auto &vals = solver.eigenvalues();
    const auto &vecs = solver.eigenvectors();

    int lo = 0, hi = N;
    if (*range == 'I' || *range == 'i') { lo = *il - 1; hi = *iu; }
    else if (*range == 'V' || *range == 'v') {
        lo = 0; hi = 0;
        for (int k = 0; k < N; ++k) {
            if (vals(k) <  *vl)  lo = k + 1;
            if (vals(k) <= *vu)  hi = k + 1;
        }
    }
    *m = hi - lo;
    for (int k = 0; k < *m; ++k) w[k] = vals(lo + k);
    if (*jobz == 'V' || *jobz == 'v') {
        Eigen::Map<MatC> Z(z, N, *m);
        Z = vecs.block(0, lo, N, *m);
    }
    return 0;
}

// ─── dsyevx: real double symmetric eigenvalue expert driver ───────────────
int dsyevx_(char *jobz, char *range, char *uplo,
            int *n, double *a, int *lda,
            double *vl, double *vu, int *il, int *iu, double *abstol,
            int *m, double *w,
            double *z, int *ldz,
            double *work, int *lwork,
            int *iwork, int *ifail,
            int *info)
{
    (void)uplo; (void)abstol; (void)work; (void)lwork;
    (void)iwork; (void)ifail;

    *info = 0;
    int N = *n;
    if (*lwork == -1) { if (work) work[0] = 64.0 * N; return 0; }

    using MatD = Eigen::Matrix<double, Eigen::Dynamic, Eigen::Dynamic, Eigen::ColMajor>;
    Eigen::Map<MatD> A(a, N, N);
    Eigen::SelfAdjointEigenSolver<MatD> solver(A);
    if (solver.info() != Eigen::Success) { *info = -1; return 0; }

    const auto &vals = solver.eigenvalues();
    const auto &vecs = solver.eigenvectors();

    int lo = 0, hi = N;
    if (*range == 'I' || *range == 'i') { lo = *il - 1; hi = *iu; }
    else if (*range == 'V' || *range == 'v') {
        lo = 0; hi = 0;
        for (int k = 0; k < N; ++k) {
            if (vals(k) <  *vl)  lo = k + 1;
            if (vals(k) <= *vu)  hi = k + 1;
        }
    }
    *m = hi - lo;
    for (int k = 0; k < *m; ++k) w[k] = vals(lo + k);
    if (*jobz == 'V' || *jobz == 'v') {
        Eigen::Map<MatD> Z(z, N, *m);
        Z = vecs.block(0, lo, N, *m);
    }
    return 0;
}

// ─── ssyevx: real float symmetric eigenvalue expert driver ────────────────
int ssyevx_(char *jobz, char *range, char *uplo,
            int *n, float *a, int *lda,
            float *vl, float *vu, int *il, int *iu, float *abstol,
            int *m, float *w,
            float *z, int *ldz,
            float *work, int *lwork,
            int *iwork, int *ifail,
            int *info)
{
    (void)uplo; (void)abstol; (void)work; (void)lwork;
    (void)iwork; (void)ifail;

    *info = 0;
    int N = *n;
    if (*lwork == -1) { if (work) work[0] = 64.f * N; return 0; }

    using MatF = Eigen::Matrix<float, Eigen::Dynamic, Eigen::Dynamic, Eigen::ColMajor>;
    Eigen::Map<MatF> A(a, N, N);
    Eigen::SelfAdjointEigenSolver<MatF> solver(A);
    if (solver.info() != Eigen::Success) { *info = -1; return 0; }

    const auto &vals = solver.eigenvalues();
    const auto &vecs = solver.eigenvectors();

    int lo = 0, hi = N;
    if (*range == 'I' || *range == 'i') { lo = *il - 1; hi = *iu; }
    else if (*range == 'V' || *range == 'v') {
        lo = 0; hi = 0;
        for (int k = 0; k < N; ++k) {
            if (vals(k) <  *vl)  lo = k + 1;
            if (vals(k) <= *vu)  hi = k + 1;
        }
    }
    *m = hi - lo;
    for (int k = 0; k < *m; ++k) w[k] = vals(lo + k);
    if (*jobz == 'V' || *jobz == 'v') {
        Eigen::Map<MatF> Z(z, N, *m);
        Z = vecs.block(0, lo, N, *m);
    }
    return 0;
}

} // extern "C"
