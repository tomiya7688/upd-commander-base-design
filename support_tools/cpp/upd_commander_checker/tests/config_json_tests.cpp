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

void test_flat_layer_config() {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_config_flat_layer";
    std::filesystem::remove_all(root);
    write_config(
        root,
        "{\"flat_layer_min_files\":14,\"flat_layer_min_direct_percent\":90}");
    const auto config = upd_checker::load_config((root / "checker").string());
    assert(config.flat_layer_min_files == 14);
    assert(config.flat_layer_min_direct_percent == 90);
    std::filesystem::remove_all(root);

    const std::vector<std::string> invalid_cases = {
        "{\"flat_layer_min_files\":0}",
        "{\"flat_layer_min_files\":true}",
        "{\"flat_layer_min_direct_percent\":0}",
        "{\"flat_layer_min_direct_percent\":101}",
        "{\"flat_layer_min_direct_percent\":80.0}",
    };
    for (const auto& content : invalid_cases) {
        const auto invalid_root =
            std::filesystem::temp_directory_path() / "upd_cpp_config_flat_layer_invalid";
        std::filesystem::remove_all(invalid_root);
        write_config(invalid_root, content);
        bool rejected = false;
        try {
            (void)upd_checker::load_config((invalid_root / "checker").string());
        } catch (const upd_checker::ConfigError&) {
            rejected = true;
        }
        assert(rejected);
        std::filesystem::remove_all(invalid_root);
    }
}

void test_model_attention_config() {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_config_model";
    std::filesystem::remove_all(root);
    write_config(
        root,
        "{\"model_group_min_items\":4,\"model_group_min_occurrences\":3}");
    const auto config = upd_checker::load_config((root / "checker").string());
    assert(config.model_group_min_items == 4);
    assert(config.model_group_min_occurrences == 3);
    std::filesystem::remove_all(root);

    const std::vector<std::string> invalid_cases = {
        "{\"model_group_min_items\":2}",
        "{\"model_group_min_items\":3.0}",
        "{\"model_group_min_occurrences\":1}",
        "{\"model_group_min_occurrences\":2.0}",
    };
    for (const auto& content : invalid_cases) {
        const auto invalid_root =
            std::filesystem::temp_directory_path() / "upd_cpp_config_model_invalid";
        std::filesystem::remove_all(invalid_root);
        write_config(invalid_root, content);
        bool rejected = false;
        try {
            (void)upd_checker::load_config((invalid_root / "checker").string());
        } catch (const upd_checker::ConfigError&) {
            rejected = true;
        }
        assert(rejected);
        std::filesystem::remove_all(invalid_root);
    }
}

void test_common_roots_config() {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_config_common_roots";
    std::filesystem::remove_all(root);
    write_config(
        root,
        "{\"common_roots\":[\"contracts\",\"Shared\",\"contracts\"]}");
    const auto config = upd_checker::load_config((root / "checker").string());
    assert(config.common_roots.size() == 2);
    assert(config.common_roots[0] == "contracts");
    assert(config.common_roots[1] == "shared");
    std::filesystem::remove_all(root);

    const auto empty_root =
        std::filesystem::temp_directory_path() / "upd_cpp_config_common_roots_empty";
    std::filesystem::remove_all(empty_root);
    write_config(empty_root, "{\"common_roots\":[]}");
    const auto empty_config =
        upd_checker::load_config((empty_root / "checker").string());
    assert(empty_config.common_roots.empty());
    std::filesystem::remove_all(empty_root);

    const std::vector<std::string> invalid_cases = {
        "{\"common_roots\":null}",
        "{\"common_roots\":\"common\"}",
        "{\"common_roots\":[1]}",
        "{\"common_roots\":[\"\"]}",
        "{\"common_roots\":[\".\"]}",
        "{\"common_roots\":[\"..\"]}",
        "{\"common_roots\":[\"ui\"]}",
        "{\"common_roots\":[\"process\"]}",
        "{\"common_roots\":[\"data\"]}",
        "{\"common_roots\":[\"nested/common\"]}",
        "{\"common_roots\":[\"nested\\\\common\"]}",
    };
    for (const auto& content : invalid_cases) {
        const auto invalid_root =
            std::filesystem::temp_directory_path() / "upd_cpp_config_common_roots_invalid";
        std::filesystem::remove_all(invalid_root);
        write_config(invalid_root, content);
        bool rejected = false;
        try {
            (void)upd_checker::load_config((invalid_root / "checker").string());
        } catch (const upd_checker::ConfigError&) {
            rejected = true;
        }
        assert(rejected);
        std::filesystem::remove_all(invalid_root);
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
    test_flat_layer_config();
    test_model_attention_config();
    test_common_roots_config();
    return 0;
}
