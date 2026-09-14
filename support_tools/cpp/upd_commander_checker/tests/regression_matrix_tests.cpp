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
    assert(has_code(findings, "UPD404"));
    std::filesystem::remove_all(root);
}

}  // namespace

int main() {
    test_commander_matrix();
    test_container_matrix();
    test_cli_ignore_matrix();
    test_ignore_file_matrix();
    test_data_type_location_matrix();
    return 0;
}
