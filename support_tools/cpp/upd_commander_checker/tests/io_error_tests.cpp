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

void restore_owner_permissions(const std::filesystem::path& path) {
    std::error_code error;
    std::filesystem::permissions(
        path,
        std::filesystem::perms::owner_all,
        std::filesystem::perm_options::replace,
        error);
}

void test_missing_ignore_file_is_normal() {
    const auto root = std::filesystem::temp_directory_path() / "upd_checker_cpp_io_missing_ignore";
    std::filesystem::remove_all(root);
    write_file(root / "plain.cpp", "void run() {}\n");

    assert(!has_code(upd_checker::scan_path(root.string(), {}), "UPD001"));
    std::filesystem::remove_all(root);
}

void test_unreadable_ignore_file_reports_upd001_and_stops_scan() {
    const auto root = std::filesystem::temp_directory_path() / "upd_checker_cpp_io_ignore";
    std::filesystem::remove_all(root);
    const auto ignore_path = root / ".updcommanderignore";
    write_file(ignore_path, "generated/**\n");
    write_file(
        root / "process" / "loop_commander.cpp",
        "void run() { for (int i = 0; i < 3; ++i) {} }\n");

    std::error_code permission_error;
    std::filesystem::permissions(
        ignore_path,
        std::filesystem::perms::none,
        std::filesystem::perm_options::replace,
        permission_error);
    if (permission_error) {
        std::filesystem::remove_all(root);
        return;
    }
    std::ifstream probe(ignore_path);
    if (probe) {
        restore_owner_permissions(ignore_path);
        std::filesystem::remove_all(root);
        return;
    }

    const auto findings = upd_checker::scan_path(root.string(), {});
    restore_owner_permissions(ignore_path);
    assert(has_code(findings, "UPD001"));
    assert(!has_code(findings, "UPD201"));
    std::filesystem::remove_all(root);
}

void test_unreadable_directory_reports_upd001_and_continues() {
    const auto root = std::filesystem::temp_directory_path() / "upd_checker_cpp_io_directory";
    std::filesystem::remove_all(root);
    const auto blocked = root / "blocked";
    write_file(blocked / "hidden.cpp", "void hidden() {}\n");
    write_file(
        root / "process" / "loop_commander.cpp",
        "void run() { for (int i = 0; i < 3; ++i) {} }\n");

    std::error_code permission_error;
    std::filesystem::permissions(
        blocked,
        std::filesystem::perms::none,
        std::filesystem::perm_options::replace,
        permission_error);
    if (permission_error) {
        std::filesystem::remove_all(root);
        return;
    }
    std::error_code probe_error;
    std::filesystem::directory_iterator probe(blocked, probe_error);
    if (!probe_error) {
        restore_owner_permissions(blocked);
        std::filesystem::remove_all(root);
        return;
    }

    const auto findings = upd_checker::scan_path(root.string(), {});
    restore_owner_permissions(blocked);
    assert(has_code(findings, "UPD001"));
    assert(has_code(findings, "UPD201"));
    std::filesystem::remove_all(root);
}

}  // namespace

int main() {
    test_missing_ignore_file_is_normal();
    test_unreadable_ignore_file_reports_upd001_and_stops_scan();
    test_unreadable_directory_reports_upd001_and_continues();
    return 0;
}
