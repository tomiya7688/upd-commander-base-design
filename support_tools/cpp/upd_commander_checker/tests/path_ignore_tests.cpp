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

void test_path_only_ignore_skips_ast_analysis() {
    const auto root = std::filesystem::temp_directory_path() / "upd_checker_cpp_path_ignore";
    std::filesystem::remove_all(root);
    write_file(root / ".updcommanderignore", "generated/**\n");
    write_file(
        root / "generated" / "process" / "loop_commander.cpp",
        "void run() { for (int i = 0; i < 3; ++i) {} }\n");

    assert(!has_code(upd_checker::scan_path(root.string(), {}), "UPD201"));
    std::filesystem::remove_all(root);
}

void test_rule_specific_ignore_keeps_ast_analysis() {
    const auto root = std::filesystem::temp_directory_path() / "upd_checker_cpp_rule_ignore";
    std::filesystem::remove_all(root);
    write_file(root / ".updcommanderignore", "UPD202 generated/**\n");
    write_file(
        root / "generated" / "process" / "loop_commander.cpp",
        "void run() { for (int i = 0; i < 3; ++i) {} }\n");

    assert(has_code(upd_checker::scan_path(root.string(), {}), "UPD201"));
    std::filesystem::remove_all(root);
}

}  // namespace

int main() {
    test_path_only_ignore_skips_ast_analysis();
    test_rule_specific_ignore_keeps_ast_analysis();
    return 0;
}
