cmake_minimum_required(VERSION 3.22)
project(aer_android CXX C)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# ── Required paths ────────────────────────────────────────────────────────────
if(NOT DEFINED AER_ROOT)
    message(FATAL_ERROR "Pass -DAER_ROOT=<path-to-qiskit-aer-src>")
endif()

set(AER_SRC     "${AER_ROOT}/src")
# Use our Android-patched wrapper instead of the upstream aer_runtime.cpp to
# fix the uint_t type conflict with Android NDK's sys/types.h.
set(AER_RUNTIME "${CMAKE_CURRENT_SOURCE_DIR}/aer_runtime_android.cpp")
set(AER_API_H   "${AER_ROOT}/contrib/runtime/aer_runtime_api.h")
set(DEPS_DIR    "${CMAKE_BINARY_DIR}/_deps")

file(MAKE_DIRECTORY "${DEPS_DIR}")

# ── Download header-only dependencies ─────────────────────────────────────────

# Eigen 3.4.0
if(NOT EXISTS "${DEPS_DIR}/eigen/Eigen/Core")
    message(STATUS "Fetching Eigen 3.4.0 ...")
    file(DOWNLOAD
        "https://gitlab.com/libeigen/eigen/-/archive/3.4.0/eigen-3.4.0.tar.gz"
        "${DEPS_DIR}/eigen.tar.gz" SHOW_PROGRESS TLS_VERIFY ON)
    execute_process(COMMAND ${CMAKE_COMMAND} -E tar xzf "${DEPS_DIR}/eigen.tar.gz"
        WORKING_DIRECTORY "${DEPS_DIR}")
    file(RENAME "${DEPS_DIR}/eigen-3.4.0" "${DEPS_DIR}/eigen")
    file(REMOVE "${DEPS_DIR}/eigen.tar.gz")
endif()

# nlohmann/json single-header
if(NOT EXISTS "${DEPS_DIR}/nlohmann/json.hpp")
    message(STATUS "Fetching nlohmann/json 3.11.3 ...")
    file(MAKE_DIRECTORY "${DEPS_DIR}/nlohmann")
    file(DOWNLOAD
        "https://github.com/nlohmann/json/releases/download/v3.11.3/json.hpp"
        "${DEPS_DIR}/nlohmann/json.hpp" SHOW_PROGRESS TLS_VERIFY ON)
endif()

# spdlog 1.13.0 (header-only)
if(NOT EXISTS "${DEPS_DIR}/spdlog/include/spdlog/spdlog.h")
    message(STATUS "Fetching spdlog 1.13.0 ...")
    file(DOWNLOAD
        "https://github.com/gabime/spdlog/archive/refs/tags/v1.13.0.tar.gz"
        "${DEPS_DIR}/spdlog.tar.gz" SHOW_PROGRESS TLS_VERIFY ON)
    execute_process(COMMAND ${CMAKE_COMMAND} -E tar xzf "${DEPS_DIR}/spdlog.tar.gz"
        WORKING_DIRECTORY "${DEPS_DIR}")
    file(RENAME "${DEPS_DIR}/spdlog-1.13.0" "${DEPS_DIR}/spdlog")
    file(REMOVE "${DEPS_DIR}/spdlog.tar.gz")
endif()

# muparserx 4.0.12 (has .cpp files)
if(NOT EXISTS "${DEPS_DIR}/muparserx/parser/mpParser.cpp")
    message(STATUS "Fetching muparserx 4.0.12 ...")
    file(DOWNLOAD
        "https://github.com/beltoforion/muparserx/archive/refs/tags/v4.0.12.tar.gz"
        "${DEPS_DIR}/muparserx.tar.gz" SHOW_PROGRESS TLS_VERIFY ON)
    execute_process(COMMAND ${CMAKE_COMMAND} -E tar xzf "${DEPS_DIR}/muparserx.tar.gz"
        WORKING_DIRECTORY "${DEPS_DIR}")
    file(RENAME "${DEPS_DIR}/muparserx-4.0.12" "${DEPS_DIR}/muparserx")
    file(REMOVE "${DEPS_DIR}/muparserx.tar.gz")
endif()
file(GLOB MUPARSERX_SOURCES "${DEPS_DIR}/muparserx/parser/*.cpp")

# Eigen BLAS (pure C++) – provides zgemm_, dgemm_, etc.
# These are "umbrella" files that set the SCALAR macro and include sub-files.
set(EIGEN_BLAS_SRCS
    "${DEPS_DIR}/eigen/blas/double.cpp"
    "${DEPS_DIR}/eigen/blas/single.cpp"
    "${DEPS_DIR}/eigen/blas/complex_double.cpp"
    "${DEPS_DIR}/eigen/blas/complex_single.cpp"
    "${DEPS_DIR}/eigen/blas/xerbla.cpp"
)

# Eigen LAPACK (pure C++) – provides zgesdd_, zgesvd_, zpotrf_, zgetrf_, etc.
# Only the top-level umbrella files: double.cpp/complex_double.cpp/etc.
# already #include the sub-files (cholesky.cpp, lu.cpp, svd.cpp, eigenvalues.cpp).
set(EIGEN_LAPACK_SRCS
    "${DEPS_DIR}/eigen/lapack/single.cpp"
    "${DEPS_DIR}/eigen/lapack/double.cpp"
    "${DEPS_DIR}/eigen/lapack/complex_single.cpp"
    "${DEPS_DIR}/eigen/lapack/complex_double.cpp"
)

# C implementations of Fortran helper routines (dlamch_, slamch_, etc.)
# + Eigen-backed implementations of missing expert drivers (zheevx_ etc.)
set(LAPACK_EXTRAS_SRCS
    "${CMAKE_CURRENT_SOURCE_DIR}/lapack_extras_android.c"
    "${CMAKE_CURRENT_SOURCE_DIR}/lapack_eigen_android.cpp"
)

# ── OpenMP ────────────────────────────────────────────────────────────────────
find_package(OpenMP)

# ── Android system libraries ──────────────────────────────────────────────────
find_library(android-lib android)
find_library(log-lib     log)

# ── Compiler flags ────────────────────────────────────────────────────────────
add_compile_options(
    -O3 -DNDEBUG -ffast-math
    -Wno-deprecated-declarations
    -Wno-unused-function
    -Wno-unused-local-typedefs
    -Wno-unused-variable
    -Wno-shadow
    -Wno-overloaded-virtual
    -Wno-float-equal
    -Wno-undef
)

add_compile_definitions(
    # Do NOT define AER_MPI at all – the guard is #ifdef AER_MPI, not #if AER_MPI
    # Use header-only spdlog / fmt
    SPDLOG_HEADER_ONLY=1
    FMT_HEADER_ONLY=1
)

if(OpenMP_CXX_FOUND)
    add_compile_options(${OpenMP_CXX_FLAGS} -static-openmp)
    add_link_options(${OpenMP_CXX_FLAGS} -static-openmp)
endif()

# ── Shared library ────────────────────────────────────────────────────────────
add_library(aer_simulator SHARED
    "${AER_RUNTIME}"
    "${CMAKE_CURRENT_SOURCE_DIR}/aer_jni.cpp"
    ${MUPARSERX_SOURCES}
    ${EIGEN_BLAS_SRCS}
    ${EIGEN_LAPACK_SRCS}
    ${LAPACK_EXTRAS_SRCS}
)

target_include_directories(aer_simulator PRIVATE
    "${AER_SRC}"
    "${AER_ROOT}/contrib/runtime"     # aer_runtime_api.h
    "${DEPS_DIR}/eigen"
    "${DEPS_DIR}/eigen/blas"          # eigen blas internal headers
    "${DEPS_DIR}"                     # nlohmann/json.hpp → #include <nlohmann/json.hpp>
    "${DEPS_DIR}/spdlog/include"
    "${DEPS_DIR}/muparserx"
    "${DEPS_DIR}/muparserx/parser"
)

target_link_libraries(aer_simulator PRIVATE
    ${android-lib}
    ${log-lib}
)

if(OpenMP_CXX_FOUND)
    target_link_libraries(aer_simulator PRIVATE OpenMP::OpenMP_CXX)
endif()

# Optional: OpenBLAS if pre-built sysroot is available
if(DEFINED AER_SYSROOT AND EXISTS "${AER_SYSROOT}/lib/libopenblas.so")
    add_library(openblas SHARED IMPORTED)
    set_target_properties(openblas PROPERTIES
        IMPORTED_LOCATION "${AER_SYSROOT}/lib/libopenblas.so")
    target_include_directories(aer_simulator PRIVATE "${AER_SYSROOT}/include")
    target_link_libraries(aer_simulator PRIVATE openblas)
    target_compile_definitions(aer_simulator PRIVATE EIGEN_USE_BLAS=1)
endif()

# ── Install ───────────────────────────────────────────────────────────────────
install(TARGETS aer_simulator
    LIBRARY DESTINATION "${CMAKE_INSTALL_PREFIX}/lib/${ANDROID_ABI}")

install(FILES "${AER_API_H}"
    DESTINATION "${CMAKE_INSTALL_PREFIX}/include")

install(FILES "${CMAKE_CURRENT_SOURCE_DIR}/com/qiskit/aer/AerSimulator.java"
    DESTINATION "${CMAKE_INSTALL_PREFIX}/java/com/qiskit/aer")
