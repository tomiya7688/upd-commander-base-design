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

void test_resolved_header_path_drives_layer_classification() {
    const auto root =
        std::filesystem::temp_directory_path() / "upd_checker_cpp_resolved_include_layer";
    std::filesystem::remove_all(root);

    write_file(root / "data" / "storage.hpp", "inline int load() { return 1; }\n");
    write_file(
        root / "ui" / "screen_processing.cpp",
        "#include \"storage.hpp\"\n"
        "int run() { return load(); }\n");
    write_file(
        root / "compile_commands.json",
        "[{\"directory\":\"" + root.generic_string() +
            "\",\"arguments\":[\"clang++\",\"-Idata\",\"-c\","
            "\"ui/screen_processing.cpp\",\"-o\",\"screen.o\"],"
            "\"file\":\"ui/screen_processing.cpp\"}]\n");

    const auto findings = upd_checker::scan_path(root.string(), {});
    assert(has_code(findings, "UPD101"));
    std::filesystem::remove_all(root);
}

void test_resolved_external_header_stays_external() {
    const auto base =
        std::filesystem::temp_directory_path() / "upd_checker_cpp_resolved_include_external";
    const auto root = base / "project";
    const auto external = base / "external";
    std::filesystem::remove_all(base);

    write_file(external / "data" / "storage.hpp", "inline int load() { return 1; }\n");
    write_file(
        root / "ui" / "screen_processing.cpp",
        "#include \"data/storage.hpp\"\n"
        "int run() { return load(); }\n");
    write_file(
        root / "compile_commands.json",
        "[{\"directory\":\"" + root.generic_string() +
            "\",\"arguments\":[\"clang++\",\"-I" + external.generic_string() +
            "\",\"-c\",\"ui/screen_processing.cpp\",\"-o\",\"screen.o\"],"
            "\"file\":\"ui/screen_processing.cpp\"}]\n");

    const auto findings = upd_checker::scan_path(root.string(), {});
    assert(!has_code(findings, "UPD101"));
    std::filesystem::remove_all(base);
}

}  // namespace

int main() {
    test_resolved_header_path_drives_layer_classification();
    test_resolved_external_header_stays_external();
    return 0;
}
