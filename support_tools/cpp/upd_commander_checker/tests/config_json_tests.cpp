#include <cassert>
#include <filesystem>
#include <fstream>
#include <string>
#include <vector>

#include "config.hpp"

namespace {

void write_config(const std::filesystem::path& root, const std::string& content) {
    std::filesystem::create_directories(root / "config");
    std::ofstream file(root / "config" / "path.json");
    file << content;
}

void assert_invalid_json(const std::string& content) {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_config_json_invalid";
    std::filesystem::remove_all(root);
    write_config(root, content);

    bool rejected = false;
    try {
        (void)upd_checker::load_config((root / "checker").string());
    } catch (const upd_checker::ConfigError& error) {
        rejected = std::string(error.what()).find("invalid config:") != std::string::npos;
    }
    assert(rejected);
    std::filesystem::remove_all(root);
}

void test_invalid_json_is_rejected() {
    const std::vector<std::string> cases = {
        "{\"input\":\".\" \"output\":\"result.txt\"}",
        "{\"input\":\".\", garbage}",
        "{\"input\":\"\\q\"}",
        "{\"input\":\"\\u12G4\"}",
        "{\"input\":\".\",}",
        "{\"unknown\":01}",
        "{\"input\":\"\\uD83Dtest\"}",
    };
    for (const auto& content : cases) {
        assert_invalid_json(content);
    }
}

void test_unicode_and_unknown_fields_are_supported() {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_config_json_unicode";
    std::filesystem::remove_all(root);
    write_config(
        root,
        "{"
        "\"input\":\".\","
        "\"output\":\"\\u7D50\\u679C-\\uD83D\\uDE80.txt\","
        "\"ignore\":[\"\\u30C6\\u30B9\\u30C8/**\"],"
        "\"warnings_as_errors\":true,"
        "\"enabled_rules\":[\"UPD101\"],"
        "\"unknown\":{\"nested\":[1,true,null]}"
        "}");

    const auto config = upd_checker::load_config((root / "checker").string());
    assert(std::filesystem::path(config.output).filename().string() == "結果-🚀.txt");
    assert(config.ignore.size() == 1);
    assert(config.ignore.front() == "テスト/**");
    assert(config.warnings_as_errors);
    assert(config.enabled_rules_configured);
    assert(config.enabled_rules.size() == 1);
    assert(config.enabled_rules.front() == "UPD101");
    std::filesystem::remove_all(root);
}

}  // namespace

int main() {
    test_invalid_json_is_rejected();
    test_unicode_and_unknown_fields_are_supported();
    return 0;
}
