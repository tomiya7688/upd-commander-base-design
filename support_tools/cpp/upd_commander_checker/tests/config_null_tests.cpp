#include <cassert>
#include <filesystem>
#include <fstream>
#include <string>
#include <utility>
#include <vector>

#include "config.hpp"

namespace {

void write_config(const std::filesystem::path& root, const std::string& content) {
    std::filesystem::create_directories(root / "config");
    std::ofstream file(root / "config" / "path.json");
    file << content;
}

void assert_invalid_field(const std::string& content, const std::string& field) {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_config_null";
    std::filesystem::remove_all(root);
    write_config(root, content);

    bool rejected = false;
    try {
        (void)upd_checker::load_config((root / "checker").string());
    } catch (const upd_checker::ConfigError& error) {
        rejected = std::string(error.what()).find("invalid config field: " + field) !=
                   std::string::npos;
    }
    assert(rejected);
    std::filesystem::remove_all(root);
}

void test_null_and_invalid_typed_fields() {
    const std::vector<std::pair<std::string, std::string>> cases = {
        {"{\"input\":null}", "input"},
        {"{\"output\":null}", "output"},
        {"{\"ignore\":null}", "ignore"},
        {"{\"ignore\":[null]}", "ignore"},
        {"{\"warnings_as_errors\":null}", "warnings_as_errors"},
        {"{\"enabled_rules\":null}", "enabled_rules"},
        {"{\"input\":1}", "input"},
        {"{\"warnings_as_errors\":\"true\"}", "warnings_as_errors"},
    };
    for (const auto& test_case : cases) {
        assert_invalid_field(test_case.first, test_case.second);
    }
}

void test_null_root() {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_config_null_root";
    std::filesystem::remove_all(root);
    write_config(root, "null");

    bool rejected = false;
    try {
        (void)upd_checker::load_config((root / "checker").string());
    } catch (const upd_checker::ConfigError& error) {
        rejected = std::string(error.what()).find("invalid config:") != std::string::npos;
    }
    assert(rejected);
    std::filesystem::remove_all(root);
}

}  // namespace

int main() {
    test_null_and_invalid_typed_fields();
    test_null_root();
    return 0;
}
