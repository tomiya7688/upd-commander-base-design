cmake_minimum_required(VERSION 3.20)
project(upd_commander_checker LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

add_library(upd_checker_core
    src/classifier.cpp
    src/config.cpp
    src/dependency_rules.cpp
    src/ignore_rules.cpp
    src/responsibility_rules.cpp
    src/scanner.cpp
)
target_include_directories(upd_checker_core PUBLIC include)

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
endif()
