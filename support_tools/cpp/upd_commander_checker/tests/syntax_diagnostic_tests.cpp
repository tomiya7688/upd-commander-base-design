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

void test_main_file_syntax_error_reports_upd002() {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_syntax_main";
    std::filesystem::remove_all(root);
    const auto source = root / "broken.cpp";
    write_file(source, "int broken( {\n");

    const auto findings = upd_checker::scan_path(source.string(), {});
    assert(has_code(findings, "UPD002"));
    std::filesystem::remove_all(root);
}

void test_valid_code_has_no_upd002() {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_syntax_valid";
    std::filesystem::remove_all(root);
    const auto source = root / "valid.cpp";
    write_file(source, "int value = 0;\n");

    const auto findings = upd_checker::scan_path(source.string(), {});
    assert(!has_code(findings, "UPD002"));
    std::filesystem::remove_all(root);
}

void test_warning_diagnostic_is_not_upd002() {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_syntax_warning";
    std::filesystem::remove_all(root);
    const auto source = root / "warning.cpp";
    write_file(source, "#warning diagnostic test\nint value = 0;\n");

    const auto findings = upd_checker::scan_path(source.string(), {});
    assert(!has_code(findings, "UPD002"));
    std::filesystem::remove_all(root);
}

void test_header_error_is_only_main_file_error_when_header_is_target() {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_syntax_header";
    std::filesystem::remove_all(root);
    const auto header = root / "broken.hpp";
    const auto source = root / "consumer.cpp";
    write_file(header, "struct Broken { int value }\n");
    write_file(source, "#include \"broken.hpp\"\nint value = 0;\n");

    const auto source_findings = upd_checker::scan_path(source.string(), {});
    assert(!has_code(source_findings, "UPD002"));

    const auto header_findings = upd_checker::scan_path(header.string(), {});
    assert(has_code(header_findings, "UPD002"));
    std::filesystem::remove_all(root);
}

}  // namespace

int main() {
    test_main_file_syntax_error_reports_upd002();
    test_valid_code_has_no_upd002();
    test_warning_diagnostic_is_not_upd002();
    test_header_error_is_only_main_file_error_when_header_is_target();
    return 0;
}
