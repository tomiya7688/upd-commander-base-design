cmake_minimum_required(VERSION 3.20)
project(upd_commander_checker LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

find_program(LLVM_CONFIG_EXECUTABLE
    NAMES llvm-config llvm-config-20 llvm-config-19 llvm-config-18 llvm-config-17 llvm-config-16)
if(LLVM_CONFIG_EXECUTABLE)
    execute_process(
        COMMAND ${LLVM_CONFIG_EXECUTABLE} --includedir
        OUTPUT_VARIABLE LLVM_INCLUDE_DIR
        OUTPUT_STRIP_TRAILING_WHITESPACE)
    execute_process(
        COMMAND ${LLVM_CONFIG_EXECUTABLE} --libdir
        OUTPUT_VARIABLE LLVM_LIBRARY_DIR
        OUTPUT_STRIP_TRAILING_WHITESPACE)
endif()

find_path(LIBCLANG_INCLUDE_DIR
    NAMES clang-c/Index.h
    HINTS ${LLVM_INCLUDE_DIR}
    PATHS
        /usr/lib/llvm-20/include
        /usr/lib/llvm-19/include
        /usr/lib/llvm-18/include
        /usr/lib/llvm-17/include
        /usr/lib/llvm-16/include)
find_library(LIBCLANG_LIBRARY
    NAMES clang libclang clang-20 clang-19 clang-18 clang-17 clang-16
    HINTS ${LLVM_LIBRARY_DIR}
    PATHS
        /usr/lib/llvm-20/lib
        /usr/lib/llvm-19/lib
        /usr/lib/llvm-18/lib
        /usr/lib/llvm-17/lib
        /usr/lib/llvm-16/lib)

if(NOT LIBCLANG_INCLUDE_DIR OR NOT LIBCLANG_LIBRARY)
    message(FATAL_ERROR "libclang development files are required for AST analysis")
endif()

add_library(upd_checker_core
    src/ast_analyzer.cpp
    src/classifier.cpp
    src/compile_arguments.cpp
    src/config.cpp
    src/data_type_location_rules.cpp
    src/dependency_rules.cpp
    src/ignore_rules.cpp
    src/rule_selection.cpp
    src/scanner.cpp
)
target_include_directories(upd_checker_core
    PUBLIC include
    PRIVATE ${LIBCLANG_INCLUDE_DIR})
target_link_libraries(upd_checker_core PUBLIC ${LIBCLANG_LIBRARY})

add_executable(upd-commander-check src/main.cpp)
target_link_libraries(upd-commander-check PRIVATE upd_checker_core)

include(CTest)
if(BUILD_TESTING)
    add_executable(upd_checker_tests tests/checker_tests.cpp)
    target_link_libraries(upd_checker_tests PRIVATE upd_checker_core)
    add_test(NAME upd_checker_tests COMMAND upd_checker_tests)

    add_executable(upd_checker_responsibility_tests tests/responsibility_tests.cpp)
    target_link_libraries(upd_checker_responsibility_tests PRIVATE upd_checker_core)
    add_test(NAME upd_checker_responsibility_tests COMMAND upd_checker_responsibility_tests)

    add_executable(upd_checker_regression_matrix_tests tests/regression_matrix_tests.cpp)
    target_link_libraries(upd_checker_regression_matrix_tests PRIVATE upd_checker_core)
    add_test(NAME upd_checker_regression_matrix_tests COMMAND upd_checker_regression_matrix_tests)
endif()
