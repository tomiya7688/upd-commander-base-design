#include <cassert>
#include <filesystem>
#include <fstream>
#include <string>

#include "classifier.hpp"
#include "scanner.hpp"

namespace {

void write_file(const std::filesystem::path& path, const std::string& content) {
    std::filesystem::create_directories(path.parent_path());
    std::ofstream file(path);
    file << content;
}

bool has_code(const std::vector<upd_checker::Finding>& findings, const std::string& code) {
    for (const auto& finding : findings) {
        if (finding.code == code) {
            return true;
        }
    }
    return false;
}

void test_ui_to_data_dependency() {
    const auto root = std::filesystem::temp_directory_path() / "upd_checker_cpp_ui_data";
    std::filesystem::remove_all(root);
    write_file(root / "ui" / "screen_processing.cpp", "#include \"data/storage.hpp\"\n");
    assert(has_code(upd_checker::scan_path(root.string(), {}), "UPD101"));
    std::filesystem::remove_all(root);
}

void test_cross_application_dependency() {
    const auto root = std::filesystem::temp_directory_path() / "upd_checker_cpp_apps";
    std::filesystem::remove_all(root);
    write_file(
        root / "applications" / "main" / "process" / "main_commander.cpp",
        "#include \"applications/settings/process/settings_processing.hpp\"\n");
    assert(has_code(upd_checker::scan_path(root.string(), {}), "UPD102"));
    std::filesystem::remove_all(root);
}

void test_data_commander_warning() {
    const auto root = std::filesystem::temp_directory_path() / "upd_checker_cpp_data_commanders";
    std::filesystem::remove_all(root);
    write_file(
        root / "data" / "save_commander.cpp",
        "#include \"data/cache_commander.hpp\"\n");
    assert(has_code(upd_checker::scan_path(root.string(), {}), "UPD103"));
    std::filesystem::remove_all(root);
}

void test_boundary_like_directory_is_not_boundary_api() {
    const auto root = std::filesystem::temp_directory_path() / "upd_checker_cpp_contractor";
    std::filesystem::remove_all(root);
    write_file(
        root / "applications" / "main" / "process" / "main_commander.cpp",
        "#include \"applications/settings/contractor/process/settings_processing.hpp\"\n");
    assert(has_code(upd_checker::scan_path(root.string(), {}), "UPD102"));
    std::filesystem::remove_all(root);
}

void test_boundary_layer_violation_keeps_upd101() {
    const auto root = std::filesystem::temp_directory_path() / "upd_checker_cpp_boundary_layer";
    std::filesystem::remove_all(root);
    write_file(
        root / "applications" / "main" / "ui" / "screen_processing.cpp",
        "#include \"applications/settings/shared/data/storage.hpp\"\n");
    const auto findings = upd_checker::scan_path(root.string(), {});
    assert(has_code(findings, "UPD101"));
    assert(!has_code(findings, "UPD102"));
    std::filesystem::remove_all(root);
}

void test_ast_ignores_comment_and_string_pseudo_syntax() {
    const auto root = std::filesystem::temp_directory_path() / "upd_checker_cpp_ast_text";
    std::filesystem::remove_all(root);
    write_file(
        root / "process" / "text_commander.cpp",
        "void run() {\n"
        "    const char* sample = \"for (int i = 0; i < 3; ++i)\";\n"
        "    // while (true) must not be treated as syntax.\n"
        "}\n");
    const auto findings = upd_checker::scan_path(root.string(), {});
    assert(!has_code(findings, "UPD201"));
    std::filesystem::remove_all(root);
}

void test_ast_detects_real_loop() {
    const auto root = std::filesystem::temp_directory_path() / "upd_checker_cpp_ast_loop";
    std::filesystem::remove_all(root);
    write_file(
        root / "process" / "loop_commander.cpp",
        "void run() { for (int i = 0; i < 3; ++i) {} }\n");
    assert(has_code(upd_checker::scan_path(root.string(), {}), "UPD201"));
    std::filesystem::remove_all(root);
}

void test_ast_detects_multiline_parameters() {
    const auto root = std::filesystem::temp_directory_path() / "upd_checker_cpp_ast_parameters";
    std::filesystem::remove_all(root);
    write_file(
        root / "process" / "math_processing.cpp",
        "void calculate(\n"
        "    int left,\n"
        "    int right) {}\n");
    assert(has_code(upd_checker::scan_path(root.string(), {}), "UPD301"));
    std::filesystem::remove_all(root);
}

void test_checker_package_name_is_not_commander() {
    const auto module = upd_checker::classify_path(
        "support_tools/cpp/upd_commander_checker/src/scanner.cpp");
    assert(module.role.empty());
}

void test_inline_ignore() {
    const auto root = std::filesystem::temp_directory_path() / "upd_checker_cpp_ignore";
    std::filesystem::remove_all(root);
    write_file(
        root / "process" / "fast_commander.cpp",
        "int run(int a, int b) { return a + b; } // upd: ignore UPD202 - performance\n");
    assert(!has_code(upd_checker::scan_path(root.string(), {}), "UPD202"));
    std::filesystem::remove_all(root);
}

}  // namespace

int main() {
    test_ui_to_data_dependency();
    test_cross_application_dependency();
    test_data_commander_warning();
    test_boundary_like_directory_is_not_boundary_api();
    test_boundary_layer_violation_keeps_upd101();
    test_ast_ignores_comment_and_string_pseudo_syntax();
    test_ast_detects_real_loop();
    test_ast_detects_multiline_parameters();
    test_checker_package_name_is_not_commander();
    test_inline_ignore();
    return 0;
}
