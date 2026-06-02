cmake_minimum_required(VERSION 3.22)
project(neat_android CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# ── Source files ──────────────────────────────────────────────────────────────
set(NEAT_CPP_DIR "${CMAKE_CURRENT_SOURCE_DIR}/../cpp/neat")

set(NEAT_SOURCES
    "${NEAT_CPP_DIR}/genome.cpp"
    "${NEAT_CPP_DIR}/network.cpp"
    "${NEAT_CPP_DIR}/species.cpp"
    "${NEAT_CPP_DIR}/pool.cpp"
    "${CMAKE_CURRENT_SOURCE_DIR}/neat_jni.cpp"
)

# ── Compiler flags ────────────────────────────────────────────────────────────
add_compile_options(
    -O3 -DNDEBUG -ffast-math
    -Wno-unused-function
    -Wno-unused-variable
)

# ── Android system libraries ──────────────────────────────────────────────────
find_library(log-lib log)

# ── Shared library ────────────────────────────────────────────────────────────
add_library(neat SHARED ${NEAT_SOURCES})

target_include_directories(neat PRIVATE "${NEAT_CPP_DIR}")

target_link_libraries(neat PRIVATE ${log-lib})

# ── Install ───────────────────────────────────────────────────────────────────
install(TARGETS neat
    LIBRARY DESTINATION "${CMAKE_INSTALL_PREFIX}/lib/${ANDROID_ABI}")

install(FILES "${CMAKE_CURRENT_SOURCE_DIR}/com/qiskit/neat/NeatEvolver.java"
    DESTINATION "${CMAKE_INSTALL_PREFIX}/java/com/qiskit/neat")

install(FILES "${NEAT_CPP_DIR}/neat.hpp"
              "${NEAT_CPP_DIR}/config.hpp"
              "${NEAT_CPP_DIR}/genome.hpp"
              "${NEAT_CPP_DIR}/network.hpp"
              "${NEAT_CPP_DIR}/species.hpp"
              "${NEAT_CPP_DIR}/pool.hpp"
    DESTINATION "${CMAKE_INSTALL_PREFIX}/include/neat")
