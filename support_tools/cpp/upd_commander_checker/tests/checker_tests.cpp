#include <cassert>
#include <filesystem>
#include <fstream>
#include <string>

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
    test_inline_ignore();
    return 0;
}
