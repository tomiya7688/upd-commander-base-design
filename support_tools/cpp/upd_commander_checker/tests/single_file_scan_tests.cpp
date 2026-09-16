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

void test_single_file_preserves_ui_layer() {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_single_ui";
    std::filesystem::remove_all(root);
    const auto dependency = root / "applications" / "main" / "data" / "storage.hpp";
    const auto target = root / "applications" / "main" / "ui" / "screen.cpp";
    write_file(dependency, "#pragma once\n");
    write_file(target, include_file(dependency));

    assert(has_code(upd_checker::scan_path(target.string(), {}), "UPD101"));
    std::filesystem::remove_all(root);
}

void test_single_file_preserves_application_boundary() {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_single_app";
    std::filesystem::remove_all(root);
    const auto dependency =
        root / "applications" / "settings" / "process" / "settings_processing.hpp";
    const auto target = root / "applications" / "main" / "process" / "main_commander.cpp";
    write_file(dependency, "#pragma once\n");
    write_file(target, include_file(dependency));

    assert(has_code(upd_checker::scan_path(target.string(), {}), "UPD102"));
    std::filesystem::remove_all(root);
}

}  // namespace

int main() {
    test_single_file_preserves_ui_layer();
    test_single_file_preserves_application_boundary();
    return 0;
}
