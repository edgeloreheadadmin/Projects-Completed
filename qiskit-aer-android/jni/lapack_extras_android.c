/*
 * C implementations of Fortran LAPACK helper routines that are not covered by
 * Eigen's C++ LAPACK back-end.  These are trivial machine-constants routines
 * whose Fortran source lives in eigen/lapack/*.f and cannot be compiled for
 * Android because the NDK has no Fortran compiler.
 *
 * Reference: LAPACK Users' Guide, 3rd Edition.
 */
#include <float.h>
#include <math.h>
#include <string.h>
#include <stddef.h>

/* ── dlamch / slamch ─────────────────────────────────────────────────────── */

/* Returns double-precision machine parameters.
 * Fortran calling convention: single character argument by reference. */
double dlamch_(const char *cmach)
{
    switch (cmach[0]) {
    case 'E': case 'e': return DBL_EPSILON;           /* relative machine epsilon */
    case 'S': case 's': return DBL_MIN;               /* safe minimum */
    case 'B': case 'b': return (double)FLT_RADIX;     /* base */
    case 'P': case 'p': return DBL_EPSILON * FLT_RADIX; /* precision = eps*base */
    case 'N': case 'n': return (double)DBL_MANT_DIG;  /* digits in mantissa */
    case 'R': case 'r': return 1.0;                   /* rounding mode (1 = round-to-nearest) */
    case 'M': case 'm': return (double)DBL_MIN_EXP;   /* minimum exponent */
    case 'U': case 'u': return DBL_MIN;               /* underflow threshold */
    case 'L': case 'l': return (double)DBL_MAX_EXP;   /* largest exponent */
    case 'O': case 'o': return DBL_MAX;               /* overflow threshold */
    default:            return 0.0;
    }
}

/* Single-precision version. */
float slamch_(const char *cmach)
{
    switch (cmach[0]) {
    case 'E': case 'e': return FLT_EPSILON;
    case 'S': case 's': return FLT_MIN;
    case 'B': case 'b': return (float)FLT_RADIX;
    case 'P': case 'p': return FLT_EPSILON * FLT_RADIX;
    case 'N': case 'n': return (float)FLT_MANT_DIG;
    case 'R': case 'r': return 1.0f;
    case 'M': case 'm': return (float)FLT_MIN_EXP;
    case 'U': case 'u': return FLT_MIN;
    case 'L': case 'l': return (float)FLT_MAX_EXP;
    case 'O': case 'o': return FLT_MAX;
    default:            return 0.0f;
    }
}

/* ── Pythagoras helpers ───────────────────────────────────────────────────── */

double dlapy2_(const double *x, const double *y) { return hypot(*x, *y); }
double dlapy3_(const double *x, const double *y, const double *z)
{ return sqrt((*x)*(*x) + (*y)*(*y) + (*z)*(*z)); }

float slapy2_(const float *x, const float *y)  { return hypotf(*x, *y); }
float slapy3_(const float *x, const float *y, const float *z)
{ return sqrtf((*x)*(*x) + (*y)*(*y) + (*z)*(*z)); }

/* ── Timing stubs (Fortran LAPACK uses SECOND/DSECND for timing) ──────────── */
float  second_ (void) { return 0.0f; }
double dsecnd_ (void) { return 0.0; }

/* ── ilaXlr / ilaXlc: last non-zero row/column index helpers ─────────────── */
/* These index the last non-zero row / column of a matrix.
 * Eigen's C++ LAPACK typically provides these but we add stubs for safety. */
