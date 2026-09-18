#include <cassert>
#include <filesystem>
#include <fstream>
#include <string>
#include <vector>

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

bool has_code_message(
    const std::vector<upd_checker::Finding>& findings,
    const std::string& code,
    const std::string& text) {
    for (const auto& finding : findings) {
        if (finding.code == code && finding.message.find(text) != std::string::npos) {
            return true;
        }
    }
    return false;
}

void test_commander_matrix() {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_matrix_commander";
    std::filesystem::remove_all(root);
    write_file(
        root / "process" / "rule_commander.cpp",
        "#include <cstdio>\n"
        "void run() { int value = 1 + 2; FILE* file = fopen(\"sample.txt\", \"r\"); (void)file; }\n");
    const auto findings = upd_checker::scan_path(root.string(), {});
    assert(has_code(findings, "UPD202"));
    assert(has_code(findings, "UPD203"));
    std::filesystem::remove_all(root);
}

void test_upd203_namespace_alias() {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_matrix_alias";
    std::filesystem::remove_all(root);
    write_file(
        root / "process" / "alias_commander.cpp",
        "#include <fstream>\n"
        "namespace io = std;\n"
        "void run() { io::ifstream input(\"sample.txt\"); }\n");
    const auto findings = upd_checker::scan_path(root.string(), {});
    assert(has_code(findings, "UPD203"));
    std::filesystem::remove_all(root);
}

void test_upd203_ignores_user_symbol_with_similar_name() {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_matrix_user_symbol";
    std::filesystem::remove_all(root);
    write_file(
        root / "process" / "local_commander.cpp",
        "void sqlite_helper() {}\n"
        "void run() { sqlite_helper(); }\n");
    const auto findings = upd_checker::scan_path(root.string(), {});
    assert(!has_code(findings, "UPD203"));
    std::filesystem::remove_all(root);
}

void test_container_matrix() {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_matrix_container";
    std::filesystem::remove_all(root);
    write_file(
        root / "process" / "large_commander.cpp",
        "#include <utility>\n"
        "struct LargeCommander {\n"
        "std::pair<int,int> run(\n"
        "int a,\nint b,\nint c,\nint d,\nint e,\nint f,\n"
        "int g,\nint h,\nint i,\nint j,\nint k,\nint l) { return {a,b}; }\n"
        "};\n");
    const auto findings = upd_checker::scan_path(root.string(), {});
    assert(has_code(findings, "UPD301"));
    assert(has_code(findings, "UPD302"));
    assert(has_code(findings, "UPD303"));
    std::filesystem::remove_all(root);
}

void test_private_method_container_rules() {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_matrix_private_container";
    std::filesystem::remove_all(root);
    write_file(
        root / "process" / "private_processing.cpp",
        "#include <utility>\n"
        "class PrivateProcessing {\n"
        "private:\n"
        "std::pair<int,int> run(int left, int right) { return {left, right}; }\n"
        "};\n");
    const auto findings = upd_checker::scan_path(root.string(), {});
    assert(has_code(findings, "UPD301"));
    assert(has_code(findings, "UPD302"));
    std::filesystem::remove_all(root);
}

void test_constructor_is_not_container_operation() {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_matrix_constructor";
    std::filesystem::remove_all(root);
    write_file(
        root / "process" / "constructor_processing.cpp",
        "class ConstructorProcessing {\n"
        "public:\n"
        "ConstructorProcessing(int left, int right) {}\n"
        "};\n");
    const auto findings = upd_checker::scan_path(root.string(), {});
    assert(!has_code(findings, "UPD301"));
    std::filesystem::remove_all(root);
}

void test_cli_ignore_matrix() {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_matrix_ignore";
    std::filesystem::remove_all(root);
    write_file(root / "process" / "ignored_commander.cpp", "int run() { return 1 + 2; }\n");
    const auto findings = upd_checker::scan_path(root.string(), {"process/**"});
    assert(!has_code(findings, "UPD202"));
    std::filesystem::remove_all(root);
}

void test_ignore_file_matrix() {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_matrix_ignore_file";
    std::filesystem::remove_all(root);
    write_file(root / "process" / "ignored_commander.cpp", "int run() { return 1 + 2; }\n");
    write_file(root / ".updcommanderignore", "UPD202 process/ignored_commander.cpp # test\n");
    const auto findings = upd_checker::scan_path(root.string(), {});
    assert(!has_code(findings, "UPD202"));
    std::filesystem::remove_all(root);
}

void test_data_type_location_matrix() {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_matrix_data";
    std::filesystem::remove_all(root);
    write_file(root / "models.hpp", "struct First { int value; }; struct Second { int value; };\n");
    auto findings = upd_checker::scan_path(root.string(), {});
    assert(has_code(findings, "UPD403"));

    write_file(root / "consumer.cpp", "#include \"models.hpp\"\nFirst value;\n");
    findings = upd_checker::scan_path(root.string(), {});
    assert(has_code_message(findings, "UPD404", "First"));
    assert(has_code_message(findings, "UPD403", "Second"));
    std::filesystem::remove_all(root);
}

void test_data_type_reference_uses_exact_declaration() {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_matrix_data_identity";
    std::filesystem::remove_all(root);
    write_file(root / "models.hpp", "struct First { int value; }; struct Second { int value; };\n");
    write_file(root / "consumer.cpp", "#include \"models.hpp\"\nSecond value;\n");
    const auto findings = upd_checker::scan_path(root.string(), {});
    assert(has_code_message(findings, "UPD403", "First"));
    assert(has_code_message(findings, "UPD404", "Second"));
    assert(!has_code_message(findings, "UPD404", "First"));
    std::filesystem::remove_all(root);
}

void test_included_header_reference_is_not_main_file_reference() {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_matrix_header_reference";
    std::filesystem::remove_all(root);
    write_file(root / "models.hpp", "struct First { int value; }; struct Second { int value; };\n");
    write_file(root / "helper.inc", "#include \"models.hpp\"\nFirst helper_value;\n");
    write_file(root / "consumer.cpp", "#include \"helper.inc\"\nint value = 0;\n");
    const auto findings = upd_checker::scan_path(root.string(), {});
    assert(has_code_message(findings, "UPD403", "First"));
    assert(!has_code_message(findings, "UPD404", "First"));
    std::filesystem::remove_all(root);
}


void write_flat_layer_files(
    const std::filesystem::path& root,
    int direct,
    int nested,
    int excluded) {
    for (int index = 0; index < direct; ++index) {
        write_file(
            root / "process" / ("direct_" + std::to_string(index) + ".cpp"),
            "int direct_" + std::to_string(index) + " = 0;\n");
    }
    for (int index = 0; index < nested; ++index) {
        write_file(
            root / "process" / "group" / ("nested_" + std::to_string(index) + ".cpp"),
            "int nested_" + std::to_string(index) + " = 0;\n");
    }
    for (int index = 0; index < excluded; ++index) {
        write_file(
            root / "process" / "generated" / ("generated_" + std::to_string(index) + ".cpp"),
            "int generated_" + std::to_string(index) + " = 0;\n");
    }
}

void test_flat_layer_default_boundary() {
    const auto below = std::filesystem::temp_directory_path() / "upd_cpp_flat_below";
    std::filesystem::remove_all(below);
    write_flat_layer_files(below, 9, 3, 0);
    assert(!has_code(upd_checker::scan_path(below.string(), {}), "UPD405"));
    std::filesystem::remove_all(below);

    const auto over = std::filesystem::temp_directory_path() / "upd_cpp_flat_over";
    std::filesystem::remove_all(over);
    write_flat_layer_files(over, 10, 2, 0);
    assert(has_code(upd_checker::scan_path(over.string(), {}), "UPD405"));
    std::filesystem::remove_all(over);
}

void test_flat_layer_small_and_generated_heavy() {
    const auto small = std::filesystem::temp_directory_path() / "upd_cpp_flat_small";
    std::filesystem::remove_all(small);
    write_flat_layer_files(small, 8, 0, 0);
    assert(!has_code(upd_checker::scan_path(small.string(), {}), "UPD405"));
    std::filesystem::remove_all(small);

    const auto generated = std::filesystem::temp_directory_path() / "upd_cpp_flat_generated";
    std::filesystem::remove_all(generated);
    write_flat_layer_files(generated, 5, 0, 20);
    assert(!has_code(upd_checker::scan_path(generated.string(), {}), "UPD405"));
    std::filesystem::remove_all(generated);
}

void test_flat_layer_custom_thresholds() {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_flat_custom";
    std::filesystem::remove_all(root);
    write_flat_layer_files(root, 6, 0, 0);
    assert(has_code(upd_checker::scan_path(root.string(), {}, 2, 6, 80), "UPD405"));
    std::filesystem::remove_all(root);
}

}  // namespace

int main() {
    test_commander_matrix();
    test_upd203_namespace_alias();
    test_upd203_ignores_user_symbol_with_similar_name();
    test_container_matrix();
    test_private_method_container_rules();
    test_constructor_is_not_container_operation();
    test_cli_ignore_matrix();
    test_ignore_file_matrix();
    test_data_type_location_matrix();
    test_data_type_reference_uses_exact_declaration();
    test_included_header_reference_is_not_main_file_reference();
    test_flat_layer_default_boundary();
    test_flat_layer_small_and_generated_heavy();
    test_flat_layer_custom_thresholds();
    return 0;
}
