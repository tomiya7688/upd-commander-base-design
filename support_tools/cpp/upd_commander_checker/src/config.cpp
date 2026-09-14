#include "config.hpp"

#include "rule_selection.hpp"

#include <cctype>
#include <filesystem>
#include <fstream>
#include <regex>
#include <sstream>

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

std::string trim(const std::string& value) {
    const auto first = value.find_first_not_of(" \t\r\n");
    if (first == std::string::npos) {
        return {};
    }
    const auto last = value.find_last_not_of(" \t\r\n");
    return value.substr(first, last - first + 1);
}

bool has_balanced_json_structure(const std::string& text) {
    bool in_string = false;
    bool escaped = false;
    int braces = 0;
    int brackets = 0;
    for (const char ch : text) {
        if (in_string) {
            if (escaped) {
                escaped = false;
            } else if (ch == '\\') {
                escaped = true;
            } else if (ch == '"') {
                in_string = false;
            }
            continue;
        }
        if (ch == '"') {
            in_string = true;
        } else if (ch == '{') {
            ++braces;
        } else if (ch == '}') {
            --braces;
        } else if (ch == '[') {
            ++brackets;
        } else if (ch == ']') {
            --brackets;
        }
        if (braces < 0 || brackets < 0) {
            return false;
        }
    }
    return !in_string && !escaped && braces == 0 && brackets == 0;
}

void validate_object_shape(const std::string& text, const std::filesystem::path& path) {
    const std::string value = trim(text);
    if (value.size() < 2 || value.front() != '{' || value.back() != '}' ||
        !has_balanced_json_structure(value)) {
        throw ConfigError("invalid config: " + path.string());
    }
}

bool contains_key(const std::string& text, const std::string& key) {
    return std::regex_search(text, std::regex("\\\"" + key + "\\\"\\s*:"));
}

std::string decode_json_string(const std::string& value) {
    std::string output;
    bool escaped = false;
    for (const char ch : value) {
        if (escaped) {
            if (ch == 'n') {
                output.push_back('\n');
            } else if (ch == 'r') {
                output.push_back('\r');
            } else if (ch == 't') {
                output.push_back('\t');
            } else {
                output.push_back(ch);
            }
            escaped = false;
        } else if (ch == '\\') {
            escaped = true;
        } else {
            output.push_back(ch);
        }
    }
    return output;
}

bool try_string_value(const std::string& text, const std::string& key, std::string& value) {
    const std::regex pattern("\\\"" + key + "\\\"\\s*:\\s*\\\"((?:\\\\.|[^\\\"])*)\\\"");
    std::smatch match;
    if (!std::regex_search(text, match, pattern)) {
        return false;
    }
    value = decode_json_string(match[1].str());
    return true;
}

bool try_bool_value(const std::string& text, const std::string& key, bool& value) {
    const std::regex pattern("\\\"" + key + "\\\"\\s*:\\s*(true|false)");
    std::smatch match;
    if (!std::regex_search(text, match, pattern)) {
        return false;
    }
    value = match[1].str() == "true";
    return true;
}

bool try_string_array(const std::string& text, const std::string& key, std::vector<std::string>& values) {
    const std::regex array_pattern("\\\"" + key + "\\\"\\s*:\\s*\\[([^\\]]*)\\]");
    std::smatch array_match;
    if (!std::regex_search(text, array_match, array_pattern)) {
        return false;
    }

    const std::string body = trim(array_match[1].str());
    if (body.empty()) {
        values.clear();
        return true;
    }

    values.clear();
    const std::regex item_pattern("\\s*\\\"((?:\\\\.|[^\\\"])*)\\\"\\s*(,|$)");
    std::size_t consumed = 0;
    for (std::sregex_iterator iterator(body.begin(), body.end(), item_pattern), end;
         iterator != end;
         ++iterator) {
        const auto& match = *iterator;
        if (static_cast<std::size_t>(match.position()) != consumed) {
            return false;
        }
        values.push_back(decode_json_string(match[1].str()));
        consumed += static_cast<std::size_t>(match.length());
    }
    return consumed == body.size();
}

std::filesystem::path find_config(const std::string& executable_path) {
    const auto executable = std::filesystem::absolute(executable_path).parent_path();
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

}  // namespace

Config load_config(const std::string& executable_path) {
    Config config;
    const auto path = find_config(executable_path);
    if (path.empty()) {
        return config;
    }

    const std::string text = read_text(path);
    validate_object_shape(text, path);

    std::string input;
    if (contains_key(text, "input") && !try_string_value(text, "input", input)) {
        throw ConfigError("invalid config field: input");
    }
    std::string output;
    if (contains_key(text, "output") && !try_string_value(text, "output", output)) {
        throw ConfigError("invalid config field: output");
    }
    std::vector<std::string> ignore;
    if (contains_key(text, "ignore") && !try_string_array(text, "ignore", ignore)) {
        throw ConfigError("invalid config field: ignore");
    }
    bool warnings_as_errors = false;
    if (contains_key(text, "warnings_as_errors") &&
        !try_bool_value(text, "warnings_as_errors", warnings_as_errors)) {
        throw ConfigError("invalid config field: warnings_as_errors");
    }
    std::vector<std::string> enabled_rules;
    const bool enabled_rules_configured = contains_key(text, "enabled_rules");
    if (enabled_rules_configured &&
        (!try_string_array(text, "enabled_rules", enabled_rules) ||
         !enabled_rules_are_valid(enabled_rules))) {
        throw ConfigError("invalid config field: enabled_rules");
    }

    const auto base = path.parent_path().parent_path();
    config.input = resolve_path(base, input.empty() ? "." : input);
    config.output = output.empty() ? "" : resolve_path(base, output);
    config.ignore = std::move(ignore);
    config.warnings_as_errors = warnings_as_errors;
    config.enabled_rules = std::move(enabled_rules);
    config.enabled_rules_configured = enabled_rules_configured;
    return config;
}

}  // namespace upd_checker
