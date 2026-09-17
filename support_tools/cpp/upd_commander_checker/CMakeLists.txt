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
    src/cli_options.cpp
    src/compile_arguments.cpp
    src/config.cpp
    src/data_type_location_rules.cpp
    src/dependency_rules.cpp
    src/ignore_rules.cpp
    src/report_output.cpp
    src/rule_selection.cpp
    src/scanner.cpp
    src/source_file_walker.cpp
    src/strict_json.cpp
    src/syntax_diagnostics.cpp
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

    add_executable(upd_checker_path_ignore_tests tests/path_ignore_tests.cpp)
    target_link_libraries(upd_checker_path_ignore_tests PRIVATE upd_checker_core)
    add_test(NAME upd_checker_path_ignore_tests COMMAND upd_checker_path_ignore_tests)

    add_executable(upd_checker_io_error_tests tests/io_error_tests.cpp)
    target_link_libraries(upd_checker_io_error_tests PRIVATE upd_checker_core)
    add_test(NAME upd_checker_io_error_tests COMMAND upd_checker_io_error_tests)

    add_executable(upd_checker_external_dependency_tests tests/external_dependency_tests.cpp)
    target_link_libraries(upd_checker_external_dependency_tests PRIVATE upd_checker_core)
    add_test(NAME upd_checker_external_dependency_tests COMMAND upd_checker_external_dependency_tests)

    add_executable(upd_checker_resolved_include_tests tests/resolved_include_tests.cpp)
    target_link_libraries(upd_checker_resolved_include_tests PRIVATE upd_checker_core)
    add_test(NAME upd_checker_resolved_include_tests COMMAND upd_checker_resolved_include_tests)

    add_executable(upd_checker_single_file_scan_tests tests/single_file_scan_tests.cpp)
    target_link_libraries(upd_checker_single_file_scan_tests PRIVATE upd_checker_core)
    add_test(NAME upd_checker_single_file_scan_tests COMMAND upd_checker_single_file_scan_tests)

    add_executable(upd_checker_shared_boundary_tests tests/shared_boundary_tests.cpp)
    target_link_libraries(upd_checker_shared_boundary_tests PRIVATE upd_checker_core)
    add_test(NAME upd_checker_shared_boundary_tests COMMAND upd_checker_shared_boundary_tests)

    add_executable(upd_checker_cli_options_tests tests/cli_options_tests.cpp)
    target_link_libraries(upd_checker_cli_options_tests PRIVATE upd_checker_core)
    add_test(NAME upd_checker_cli_options_tests COMMAND upd_checker_cli_options_tests)

    add_executable(upd_checker_config_null_tests tests/config_null_tests.cpp)
    target_link_libraries(upd_checker_config_null_tests PRIVATE upd_checker_core)
    add_test(NAME upd_checker_config_null_tests COMMAND upd_checker_config_null_tests)

    add_executable(upd_checker_config_json_tests tests/config_json_tests.cpp)
    target_link_libraries(upd_checker_config_json_tests PRIVATE upd_checker_core)
    add_test(NAME upd_checker_config_json_tests COMMAND upd_checker_config_json_tests)

    add_executable(upd_checker_syntax_diagnostic_tests tests/syntax_diagnostic_tests.cpp)
    target_link_libraries(upd_checker_syntax_diagnostic_tests PRIVATE upd_checker_core)
    add_test(NAME upd_checker_syntax_diagnostic_tests COMMAND upd_checker_syntax_diagnostic_tests)

    add_executable(upd_checker_report_output_tests tests/report_output_tests.cpp)
    target_link_libraries(upd_checker_report_output_tests PRIVATE upd_checker_core)
    add_test(NAME upd_checker_report_output_tests COMMAND upd_checker_report_output_tests)
endif()
