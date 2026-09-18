#include "config.hpp"

#include "executable_path.hpp"
#include "rule_selection.hpp"
#include "strict_json.hpp"

#include <algorithm>
#include <filesystem>
#include <fstream>
#include <limits>
#include <sstream>
#include <stdexcept>

namespace upd_checker {
namespace {

std::string read_text(const std::filesystem::path& path) {
    std::ifstream file(path);
    if (!file) {
        throw ConfigError("invalid config: " + path.string());
    }
    std::ostringstream stream;
    stream << file.rdbuf();
    return stream.str();
}

const JsonValue* find_field(const JsonValue& root, const std::string& name) {
    const auto iterator = root.object_value.find(name);
    return iterator == root.object_value.end() ? nullptr : &iterator->second;
}

std::string read_string_field(const JsonValue& root, const std::string& name) {
    const JsonValue* value = find_field(root, name);
    if (value == nullptr) {
        return {};
    }
    if (value->type != JsonValue::Type::string) {
        throw ConfigError("invalid config field: " + name);
    }
    return value->string_value;
}

bool read_bool_field(const JsonValue& root, const std::string& name) {
    const JsonValue* value = find_field(root, name);
    if (value == nullptr) {
        return false;
    }
    if (value->type != JsonValue::Type::boolean) {
        throw ConfigError("invalid config field: " + name);
    }
    return value->boolean_value;
}

int read_positive_int_field(
    const JsonValue& root,
    const std::string& name,
    int default_value) {
    const JsonValue* value = find_field(root, name);
    if (value == nullptr) {
        return default_value;
    }
    if (value->type != JsonValue::Type::number ||
        value->number_value.empty() ||
        !std::all_of(
            value->number_value.begin(),
            value->number_value.end(),
            [](char ch) { return ch >= '0' && ch <= '9'; })) {
        throw ConfigError("invalid config field: " + name);
    }

    try {
        const long long parsed = std::stoll(value->number_value);
        if (parsed < 1 || parsed > std::numeric_limits<int>::max()) {
            throw ConfigError("invalid config field: " + name);
        }
        return static_cast<int>(parsed);
    } catch (const std::invalid_argument&) {
        throw ConfigError("invalid config field: " + name);
    } catch (const std::out_of_range&) {
        throw ConfigError("invalid config field: " + name);
    }
}

std::vector<std::string> read_string_array_field(
    const JsonValue& root,
    const std::string& name,
    bool* configured = nullptr) {
    const JsonValue* value = find_field(root, name);
    if (configured != nullptr) {
        *configured = value != nullptr;
    }
    if (value == nullptr) {
        return {};
    }
    if (value->type != JsonValue::Type::array) {
        throw ConfigError("invalid config field: " + name);
    }

    std::vector<std::string> values;
    values.reserve(value->array_value.size());
    for (const auto& item : value->array_value) {
        if (item.type != JsonValue::Type::string) {
            throw ConfigError("invalid config field: " + name);
        }
        values.push_back(item.string_value);
    }
    return values;
}

std::filesystem::path find_config(const std::string& executable_path) {
    const auto executable = resolve_executable_path(executable_path).parent_path();
    const auto executable_config = executable / "config" / "path.json";
    if (std::filesystem::is_regular_file(executable_config)) {
        return executable_config;
    }
    const auto current_config = std::filesystem::current_path() / "config" / "path.json";
    if (std::filesystem::is_regular_file(current_config)) {
        return current_config;
    }
    return {};
}

std::string resolve_path(const std::filesystem::path& base, const std::string& value) {
    const std::filesystem::path path(value);
    if (path.is_absolute()) {
        return path.string();
    }
    return std::filesystem::absolute(base / path).string();
}

JsonValue parse_config_document(const std::string& text, const std::filesystem::path& path) {
    try {
        JsonValue root = parse_json(text);
        if (root.type != JsonValue::Type::object) {
            throw ConfigError("invalid config: " + path.string());
        }
        return root;
    } catch (const std::invalid_argument&) {
        throw ConfigError("invalid config: " + path.string());
    }
}

}  // namespace

Config load_config(const std::string& executable_path) {
    Config config;
    const auto path = find_config(executable_path);
    if (path.empty()) {
        return config;
    }

    const JsonValue root = parse_config_document(read_text(path), path);
    const std::string input = read_string_field(root, "input");
    const std::string output = read_string_field(root, "output");
    const std::vector<std::string> ignore = read_string_array_field(root, "ignore");
    const bool warnings_as_errors = read_bool_field(root, "warnings_as_errors");
    const int upd301_max_inputs = read_positive_int_field(root, "upd301_max_inputs", 2);
    const int flat_layer_min_files = read_positive_int_field(root, "flat_layer_min_files", 12);
    const int flat_layer_min_direct_percent =
        read_positive_int_field(root, "flat_layer_min_direct_percent", 80);
    if (flat_layer_min_direct_percent > 100) {
        throw ConfigError("invalid config field: flat_layer_min_direct_percent");
    }
    bool enabled_rules_configured = false;
    std::vector<std::string> enabled_rules =
        read_string_array_field(root, "enabled_rules", &enabled_rules_configured);
    if (enabled_rules_configured && !enabled_rules_are_valid(enabled_rules)) {
        throw ConfigError("invalid config field: enabled_rules");
    }

    const auto base = path.parent_path().parent_path();
    config.input = resolve_path(base, input.empty() ? "." : input);
    config.output = output.empty() ? "" : resolve_path(base, output);
    config.ignore = ignore;
    config.warnings_as_errors = warnings_as_errors;
    config.upd301_max_inputs = upd301_max_inputs;
    config.flat_layer_min_files = flat_layer_min_files;
    config.flat_layer_min_direct_percent = flat_layer_min_direct_percent;
    config.enabled_rules = std::move(enabled_rules);
    config.enabled_rules_configured = enabled_rules_configured;
    return config;
}

}  // namespace upd_checker
