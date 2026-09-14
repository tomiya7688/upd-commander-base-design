#include <cassert>
#include <filesystem>
#include <fstream>
#include <string>

#include "scanner.hpp"

namespace {

bool has_upd401(const std::filesystem::path& path) {
    const auto findings = upd_checker::scan_path(path.string(), {});
    for (const auto& finding : findings) {
        if (finding.code == "UPD401") {
            return true;
        }
    }
    return false;
}

void write_line_class(const std::filesystem::path& path, int total_lines) {
    std::ofstream file(path);
    file << "class Example {\n";
    for (int line = 0; line < total_lines - 2; ++line) {
        file << "// line " << line << "\n";
    }
    file << "};\n";
}

void write_method_class(const std::filesystem::path& path, int methods) {
    std::ofstream file(path);
    file << "class Example {\npublic:\n";
    for (int index = 0; index < methods; ++index) {
        file << "    void method_" << index << "() {}\n";
    }
    file << "};\n";
}

}  // namespace

int main() {
    const auto root = std::filesystem::temp_directory_path() / "upd_checker_cpp_responsibility";
    std::filesystem::remove_all(root);
    std::filesystem::create_directories(root);

    const auto allowed_lines = root / "allowed_lines.cpp";
    const auto warning_lines = root / "warning_lines.cpp";
    const auto allowed_methods = root / "allowed_methods.cpp";
    const auto warning_methods = root / "warning_methods.cpp";

    write_line_class(allowed_lines, 250);
    write_line_class(warning_lines, 251);
    write_method_class(allowed_methods, 12);
    write_method_class(warning_methods, 13);

    assert(!has_upd401(allowed_lines));
    assert(has_upd401(warning_lines));
    assert(!has_upd401(allowed_methods));
    assert(has_upd401(warning_methods));

    std::filesystem::remove_all(root);
    return 0;
}
