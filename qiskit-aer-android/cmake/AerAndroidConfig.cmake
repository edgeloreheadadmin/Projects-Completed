# Android-specific overrides applied on top of Qiskit Aer's CMakeLists.txt
# Included via -DCMAKE_PROJECT_INCLUDE when invoking cmake

# Suppress Python extension machinery — we build a plain shared library
set(AER_PYTHON_EXTENSION OFF)
set(BUILD_SHARED_LIBS ON)

# No CUDA on Android
set(AER_THRUST_BACKEND "OMP" CACHE STRING "" FORCE)
set(AER_DISABLE_GDR ON CACHE BOOL "" FORCE)

# No MKL on Android
set(AER_MKL_PATH "" CACHE STRING "" FORCE)
set(AER_BLAS_LIB_PATH "${AER_SYSROOT}/lib" CACHE PATH "" FORCE)

# Use the OpenMP from the NDK
set(OpenMP_C_FLAGS   "-fopenmp -static-openmp")
set(OpenMP_CXX_FLAGS "-fopenmp -static-openmp")
set(OpenMP_C_LIB_NAMES   "omp")
set(OpenMP_CXX_LIB_NAMES "omp")
set(OpenMP_omp_LIBRARY "${ANDROID_TOOLCHAIN_ROOT}/lib64/clang/${CMAKE_C_COMPILER_VERSION}/lib/linux/aarch64/libomp.a")

# Tell Eigen not to use BLAS separately (we handle it)
set(EIGEN_USE_BLAS OFF)

# Android log library
find_library(log-lib log)
