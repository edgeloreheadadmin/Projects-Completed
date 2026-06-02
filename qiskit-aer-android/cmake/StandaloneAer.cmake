# Standalone CMakeLists for building qiskit-aer C++ backend as a shared library
# for Android without needing a Python installation.
# Usage:
#   cmake -B build -S . \
#     -DCMAKE_TOOLCHAIN_FILE=<NDK>/build/cmake/android.toolchain.cmake \
#     -DANDROID_ABI=arm64-v8a \
#     -DANDROID_PLATFORM=android-24 \
#     -DAER_SRC_DIR=<path-to-qiskit-aer>/src \
#     -DAER_SYSROOT=<path-to-sysroot>

cmake_minimum_required(VERSION 3.18)
project(qiskit_aer_android CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

if(NOT DEFINED AER_SRC_DIR)
    message(FATAL_ERROR "AER_SRC_DIR must point to qiskit-aer/src")
endif()

# ── Includes ──────────────────────────────────────────────────────────────────
include_directories(
    ${AER_SRC_DIR}
    ${AER_SRC_DIR}/simulators
    ${AER_SRC_DIR}/framework
)

# ── Eigen ─────────────────────────────────────────────────────────────────────
set(EIGEN_URL "https://gitlab.com/libeigen/eigen/-/archive/3.4.0/eigen-3.4.0.tar.gz")
set(EIGEN_DIR "${CMAKE_BINARY_DIR}/eigen")
if(NOT EXISTS "${EIGEN_DIR}/Eigen/Core")
    message(STATUS "Downloading Eigen 3.4.0 ...")
    file(DOWNLOAD "${EIGEN_URL}" "${CMAKE_BINARY_DIR}/eigen.tar.gz" SHOW_PROGRESS)
    execute_process(
        COMMAND ${CMAKE_COMMAND} -E tar xzf "${CMAKE_BINARY_DIR}/eigen.tar.gz"
        WORKING_DIRECTORY "${CMAKE_BINARY_DIR}"
    )
    file(RENAME "${CMAKE_BINARY_DIR}/eigen-3.4.0" "${EIGEN_DIR}")
endif()
include_directories(${EIGEN_DIR})

# ── nlohmann/json ─────────────────────────────────────────────────────────────
set(JSON_URL "https://github.com/nlohmann/json/releases/download/v3.11.3/json.hpp")
set(JSON_DIR "${CMAKE_BINARY_DIR}/nlohmann")
if(NOT EXISTS "${JSON_DIR}/json.hpp")
    file(MAKE_DIRECTORY "${JSON_DIR}")
    file(DOWNLOAD "${JSON_URL}" "${JSON_DIR}/json.hpp" SHOW_PROGRESS)
endif()
include_directories(${CMAKE_BINARY_DIR})

# ── spdlog ────────────────────────────────────────────────────────────────────
set(SPDLOG_URL "https://github.com/gabime/spdlog/archive/refs/tags/v1.13.0.tar.gz")
set(SPDLOG_DIR "${CMAKE_BINARY_DIR}/spdlog-1.13.0")
if(NOT EXISTS "${SPDLOG_DIR}/include/spdlog/spdlog.h")
    file(DOWNLOAD "${SPDLOG_URL}" "${CMAKE_BINARY_DIR}/spdlog.tar.gz" SHOW_PROGRESS)
    execute_process(
        COMMAND ${CMAKE_COMMAND} -E tar xzf "${CMAKE_BINARY_DIR}/spdlog.tar.gz"
        WORKING_DIRECTORY "${CMAKE_BINARY_DIR}"
    )
endif()
include_directories(${SPDLOG_DIR}/include)

# ── muparserx ─────────────────────────────────────────────────────────────────
set(MUPARSERX_URL "https://github.com/beltoforion/muparserx/archive/refs/tags/v4.0.12.tar.gz")
set(MUPARSERX_SRC "${CMAKE_BINARY_DIR}/muparserx-4.0.12/parser")
if(NOT EXISTS "${MUPARSERX_SRC}/mpParser.cpp")
    file(DOWNLOAD "${MUPARSERX_URL}" "${CMAKE_BINARY_DIR}/muparserx.tar.gz" SHOW_PROGRESS)
    execute_process(
        COMMAND ${CMAKE_COMMAND} -E tar xzf "${CMAKE_BINARY_DIR}/muparserx.tar.gz"
        WORKING_DIRECTORY "${CMAKE_BINARY_DIR}"
    )
endif()
file(GLOB MUPARSERX_SOURCES "${MUPARSERX_SRC}/*.cpp")
include_directories(${CMAKE_BINARY_DIR}/muparserx-4.0.12)

# ── OpenBLAS (optional) ───────────────────────────────────────────────────────
if(DEFINED AER_SYSROOT AND EXISTS "${AER_SYSROOT}/lib/libopenblas.so")
    add_library(openblas SHARED IMPORTED)
    set_target_properties(openblas PROPERTIES
        IMPORTED_LOCATION "${AER_SYSROOT}/lib/libopenblas.so"
    )
    include_directories("${AER_SYSROOT}/include")
    set(AER_HAS_OPENBLAS TRUE)
endif()

# ── OpenMP ────────────────────────────────────────────────────────────────────
find_package(OpenMP)

# ── Compiler flags ────────────────────────────────────────────────────────────
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -O3 -DNDEBUG -ffast-math")
if(OpenMP_CXX_FOUND)
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} ${OpenMP_CXX_FLAGS}")
endif()

# Silence Android-specific warnings
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -Wno-deprecated-declarations -Wno-unused-function")

# ── Collect Aer framework sources ─────────────────────────────────────────────
# Aer is mostly header-only; only a few .cpp files need explicit compilation.
file(GLOB_RECURSE AER_CPP_SOURCES
    "${AER_SRC_DIR}/*.cpp"
)
# Exclude any Python-binding sources
list(FILTER AER_CPP_SOURCES EXCLUDE REGEX ".*pybind.*")
list(FILTER AER_CPP_SOURCES EXCLUDE REGEX ".*python.*")

# ── Target: libaer_simulator ──────────────────────────────────────────────────
add_library(aer_simulator SHARED
    ${AER_CPP_SOURCES}
    ${MUPARSERX_SOURCES}
    ${CMAKE_SOURCE_DIR}/../jni/aer_jni.cpp
)

target_compile_definitions(aer_simulator PRIVATE
    SPDLOG_COMPILED_LIB=0
    AER_THRUST_BACKEND_NONE=1
    DISABLE_CONAN=1
)

target_include_directories(aer_simulator PRIVATE
    ${AER_SRC_DIR}
    ${EIGEN_DIR}
    ${JSON_DIR}/..
    ${SPDLOG_DIR}/include
    ${CMAKE_BINARY_DIR}/muparserx-4.0.12
    ${MUPARSERX_SRC}
)

target_link_libraries(aer_simulator PRIVATE
    log
    android
)

if(OpenMP_CXX_FOUND)
    target_link_libraries(aer_simulator PRIVATE OpenMP::OpenMP_CXX)
endif()

if(AER_HAS_OPENBLAS)
    target_link_libraries(aer_simulator PRIVATE openblas)
endif()

# ── Install ───────────────────────────────────────────────────────────────────
install(TARGETS aer_simulator
    LIBRARY DESTINATION lib/${ANDROID_ABI}
)
