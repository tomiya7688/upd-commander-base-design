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
        {"{\"fail_on\":null}", "fail_on"},
        {"{\"fail_on\":[\"fatal\"]}", "fail_on"},
        {"{\"severity_overrides\":null}", "severity_overrides"},
        {"{\"severity_overrides\":{\"UPD101\":\"fatal\"}}", "severity_overrides"},
        {"{\"severity_overrides\":{\"bad\":\"error\"}}", "severity_overrides"},
        {"{\"gate_exceptions\":null}", "gate_exceptions"},
        {"{\"gate_exceptions\":[{\"rule\":\"UPD203\",\"path\":\"../a.cpp\",\"reason\":\"bad\"}]}", "gate_exceptions"},
        {"{\"gate_exceptions\":[{\"rule\":\"UPD203\",\"path\":\"a.cpp\",\"reason\":\"ok\",\"extra\":1}]}", "gate_exceptions"},
        {"{\"enabled_rules\":null}", "enabled_rules"},
        {"{\"input\":1}", "input"},
        {"{\"warnings_as_errors\":\"true\"}", "warnings_as_errors"},
    };
    for (const auto& test_case : cases) {
        assert_invalid_field(test_case.first, test_case.second);
    }
}

void test_valid_gate_policy_config() {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_config_gate_policy";
    std::filesystem::remove_all(root);
    write_config(
        root,
        "{\"fail_on\":[\" ERROR \",\"warning\",\"error\"],"
        "\"severity_overrides\":{\"upd203\":\"WARNING\"},"
        "\"gate_exceptions\":[{\"rule\":\"upd203\",\"path\":\"src/a.cpp\","
        "\"line\":8,\"reason\":\"approved\"}]}");
    const auto config = upd_checker::load_config((root / "checker").string());
    assert(config.fail_on_configured);
    assert((config.fail_on == std::vector<std::string>{"error", "warning"}));
    assert(config.severity_overrides.at("UPD203") == "warning");
    assert(config.gate_exceptions.size() == 1);
    assert(config.gate_exceptions.front().rule == "UPD203");
    assert(config.gate_exceptions.front().line == 8);
    assert(config.gate_exceptions.front().reason == "approved");
    std::filesystem::remove_all(root);

    const auto empty_root =
        std::filesystem::temp_directory_path() / "upd_cpp_config_gate_policy_empty";
    std::filesystem::remove_all(empty_root);
    write_config(empty_root, "{\"fail_on\":[]}");
    const auto empty_config = upd_checker::load_config((empty_root / "checker").string());
    assert(empty_config.fail_on_configured);
    assert(empty_config.fail_on.empty());
    std::filesystem::remove_all(empty_root);
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
    test_valid_gate_policy_config();
    test_null_root();
    return 0;
}
