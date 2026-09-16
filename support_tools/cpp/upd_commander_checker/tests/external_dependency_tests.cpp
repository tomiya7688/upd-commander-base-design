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

void test_external_data_header_does_not_trigger_layer_rule() {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_external_project";
    const auto external = std::filesystem::temp_directory_path() / "upd_cpp_external_lib";
    std::filesystem::remove_all(root);
    std::filesystem::remove_all(external);
    const auto header = external / "data" / "client.hpp";
    write_file(header, "#pragma once\n");
    write_file(root / "ui" / "view.cpp", include_file(header));

    assert(!has_code(upd_checker::scan_path(root.string(), {}), "UPD101"));
    std::filesystem::remove_all(root);
    std::filesystem::remove_all(external);
}

void test_external_processing_header_does_not_trigger_role_rule() {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_external_role_project";
    const auto external = std::filesystem::temp_directory_path() / "upd_cpp_external_role_lib";
    std::filesystem::remove_all(root);
    std::filesystem::remove_all(external);
    const auto header = external / "processing" / "engine.hpp";
    write_file(header, "#pragma once\n");
    write_file(root / "process" / "event_messenger.cpp", include_file(header));

    assert(!has_code(upd_checker::scan_path(root.string(), {}), "UPD101"));
    std::filesystem::remove_all(root);
    std::filesystem::remove_all(external);
}

void test_external_other_application_header_does_not_trigger_boundary_rule() {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_external_app_project";
    const auto external = std::filesystem::temp_directory_path() / "upd_cpp_external_app_lib";
    std::filesystem::remove_all(root);
    std::filesystem::remove_all(external);
    const auto header = external / "applications" / "other" / "data" / "client.hpp";
    write_file(header, "#pragma once\n");
    write_file(
        root / "applications" / "main" / "process" / "main_commander.cpp",
        include_file(header));

    assert(!has_code(upd_checker::scan_path(root.string(), {}), "UPD102"));
    std::filesystem::remove_all(root);
    std::filesystem::remove_all(external);
}

}  // namespace

int main() {
    test_external_data_header_does_not_trigger_layer_rule();
    test_external_processing_header_does_not_trigger_role_rule();
    test_external_other_application_header_does_not_trigger_boundary_rule();
    return 0;
}
