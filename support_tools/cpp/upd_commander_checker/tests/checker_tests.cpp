#include <cassert>
#include <filesystem>
#include <fstream>
#include <string>

#include "classifier.hpp"
#include "config.hpp"
#include "rule_selection.hpp"
#include "scanner.hpp"

namespace {

void write_file(const std::filesystem::path& path, const std::string& content) {
    std::filesystem::create_directories(path.parent_path());
    std::ofstream file(path);
    file << content;
}

std::string include_file(const std::filesystem::path& path) {
    return "#include \"" + path.generic_string() + "\"\n";
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
    const auto target = root / "data" / "storage.hpp";
    write_file(target, "#pragma once\n");
    write_file(root / "ui" / "screen_processing.cpp", include_file(target));
    assert(has_code(upd_checker::scan_path(root.string(), {}), "UPD101"));
    std::filesystem::remove_all(root);
}

void test_cross_application_dependency() {
    const auto root = std::filesystem::temp_directory_path() / "upd_checker_cpp_apps";
    std::filesystem::remove_all(root);
    const auto target = root / "applications" / "settings" / "process" / "settings_processing.hpp";
    write_file(target, "#pragma once\n");
    write_file(
        root / "applications" / "main" / "process" / "main_commander.cpp",
        include_file(target));
    assert(has_code(upd_checker::scan_path(root.string(), {}), "UPD102"));
    std::filesystem::remove_all(root);
}

void test_nested_application_dependency() {
    const auto root = std::filesystem::temp_directory_path() / "upd_checker_cpp_nested_apps";
    std::filesystem::remove_all(root);
    const auto target =
        root / "apps" / "product" / "applications" / "profile" / "process" /
        "profile_processing.hpp";
    write_file(target, "#pragma once\n");
    write_file(
        root / "apps" / "product" / "applications" / "settings" / "ui" /
            "screen_processing.cpp",
        include_file(target));
    assert(has_code(upd_checker::scan_path(root.string(), {}), "UPD102"));
    std::filesystem::remove_all(root);
}

void test_nested_application_classifier_uses_nearest_scope() {
    const auto module = upd_checker::classify_path(
        "apps/product/ui/commander/applications/settings/data/screen.cpp");
    assert(module.application_id == "settings");
    assert(module.layer == "data");
    assert(module.role.empty());

    const auto dependency = upd_checker::classify_include(
        "apps/product/ui/commander/applications/profile/process/profile_processing.hpp");
    assert(dependency.application_id == "profile");
    assert(dependency.layer == "process");
    assert(dependency.role == "processing");
}

void test_data_commander_warning() {
    const auto root = std::filesystem::temp_directory_path() / "upd_checker_cpp_data_commanders";
    std::filesystem::remove_all(root);
    const auto target = root / "data" / "cache_commander.hpp";
    write_file(target, "#pragma once\n");
    write_file(root / "data" / "save_commander.cpp", include_file(target));
    assert(has_code(upd_checker::scan_path(root.string(), {}), "UPD103"));
    std::filesystem::remove_all(root);
}

void test_boundary_like_directory_is_not_boundary_api() {
    const auto root = std::filesystem::temp_directory_path() / "upd_checker_cpp_contractor";
    std::filesystem::remove_all(root);
    const auto target = root / "applications" / "settings" / "contractor" / "process" /
                        "settings_processing.hpp";
    write_file(target, "#pragma once\n");
    write_file(
        root / "applications" / "main" / "process" / "main_commander.cpp",
        include_file(target));
    assert(has_code(upd_checker::scan_path(root.string(), {}), "UPD102"));
    std::filesystem::remove_all(root);
}

void test_boundary_layer_violation_keeps_upd101() {
    const auto root = std::filesystem::temp_directory_path() / "upd_checker_cpp_boundary_layer";
    std::filesystem::remove_all(root);
    const auto target =
        root / "applications" / "settings" / "shared" / "data" / "storage.hpp";
    write_file(target, "#pragma once\n");
    write_file(
        root / "applications" / "main" / "ui" / "screen_processing.cpp",
        include_file(target));
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
    write_file(root / "process" / "loop_commander.cpp", "void run() { for (int i = 0; i < 3; ++i) {} }\n");
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

void test_compile_commands_arguments_are_used() {
    const auto root = std::filesystem::temp_directory_path() / "upd_checker_cpp_compile_commands";
    std::filesystem::remove_all(root);
    write_file(
        root / "custom" / "feature.hpp",
        "#ifdef PROJECT_FEATURE\n"
        "#define FEATURE_FLAG 1\n"
        "constexpr int feature_value = 1;\n"
        "#else\n"
        "#define FEATURE_FLAG 0\n"
        "#endif\n");
    write_file(
        root / "process" / "feature_commander.cpp",
        "#include \"feature.hpp\"\n"
        "#if FEATURE_FLAG\n"
        "int run() { return feature_value + 1; }\n"
        "#else\n"
        "int run() { return 0; }\n"
        "#endif\n");
    write_file(
        root / "compile_commands.json",
        "[{\"directory\":\"" + root.generic_string() +
            "\",\"arguments\":[\"clang++\",\"-Icustom\",\"-DPROJECT_FEATURE=1\",\"-c\","
            "\"process/feature_commander.cpp\",\"-o\",\"feature.o\"],"
            "\"file\":\"process/feature_commander.cpp\"}]\n");
    assert(has_code(upd_checker::scan_path(root.string(), {}), "UPD202"));
    std::filesystem::remove_all(root);
}

void test_header_uses_related_compile_command() {
    const auto root = std::filesystem::temp_directory_path() / "upd_checker_cpp_header_commands";
    std::filesystem::remove_all(root);
    write_file(
        root / "process" / "header_commander.hpp",
        "#ifdef HEADER_FEATURE\n"
        "inline int run() { return 1 + 2; }\n"
        "#else\n"
        "inline int run() { return 0; }\n"
        "#endif\n");
    write_file(root / "driver.cpp", "#include \"process/header_commander.hpp\"\n");
    write_file(
        root / "compile_commands.json",
        "[{\"directory\":\"" + root.generic_string() +
            "\",\"arguments\":[\"clang++\",\"-DHEADER_FEATURE=1\",\"-c\",\"driver.cpp\"],"
            "\"file\":\"driver.cpp\"}]\n");
    assert(has_code(upd_checker::scan_path((root / "process" / "header_commander.hpp").string(), {}), "UPD202"));
    std::filesystem::remove_all(root);
}

void test_checker_package_name_is_not_commander() {
    const auto module = upd_checker::classify_path("support_tools/cpp/upd_commander_checker/src/scanner.cpp");
    assert(module.role.empty());
}

void test_enabled_rules_load_from_config() {
    const auto root = std::filesystem::temp_directory_path() / "upd_checker_cpp_enabled_rules";
    std::filesystem::remove_all(root);
    write_file(root / "config" / "path.json", "{\"enabled_rules\":[\"UPD101\",\"UPD202\"]}\n");
    const auto config = upd_checker::load_config((root / "checker.exe").string());
    assert(config.enabled_rules_configured);
    assert(config.enabled_rules.size() == 2);
    assert(config.enabled_rules.front() == "UPD101");
    std::filesystem::remove_all(root);
}

void test_enabled_rules_filter_findings() {
    const std::vector<upd_checker::Finding> findings = {
        {"first.cpp", 1, "UPD101", "message", "error"},
        {"second.cpp", 2, "UPD202", "message", "warning"}};
    const std::vector<std::string> enabled = {"UPD202"};
    const std::vector<std::string> none;
    const auto filtered = upd_checker::filter_enabled_findings({findings, enabled, true});
    assert(filtered.size() == 1);
    assert(filtered.front().code == "UPD202");
    assert(upd_checker::filter_enabled_findings({findings, none, true}).empty());
    assert(upd_checker::filter_enabled_findings({findings, none, false}).size() == 2);
    assert(!upd_checker::enabled_rules_are_valid({"UPD999"}));
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
    test_nested_application_dependency();
    test_nested_application_classifier_uses_nearest_scope();
    test_data_commander_warning();
    test_boundary_like_directory_is_not_boundary_api();
    test_boundary_layer_violation_keeps_upd101();
    test_ast_ignores_comment_and_string_pseudo_syntax();
    test_ast_detects_real_loop();
    test_ast_detects_multiline_parameters();
    test_compile_commands_arguments_are_used();
    test_header_uses_related_compile_command();
    test_checker_package_name_is_not_commander();
    test_inline_ignore();
    test_enabled_rules_filter_findings();
    test_enabled_rules_load_from_config();
    return 0;
}
