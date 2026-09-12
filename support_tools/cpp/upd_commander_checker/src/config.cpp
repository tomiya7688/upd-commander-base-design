#include "config.hpp"

#include <filesystem>
#include <fstream>
#include <regex>
#include <sstream>

namespace upd_checker {
namespace {

std::string read_text(const std::filesystem::path& path) {
    std::ifstream file(path);
    if (!file) {
        return {};
    }
    std::ostringstream stream;
    stream << file.rdbuf();
    return stream.str();
}

std::string string_value(const std::string& text, const std::string& key) {
    const std::regex pattern("\\\"" + key + "\\\"\\s*:\\s*\\\"([^\\\"]*)\\\"");
    std::smatch match;
    return std::regex_search(text, match, pattern) ? match[1].str() : std::string{};
}

bool bool_value(const std::string& text, const std::string& key) {
    const std::regex pattern("\\\"" + key + "\\\"\\s*:\\s*(true|false)");
    std::smatch match;
    return std::regex_search(text, match, pattern) && match[1].str() == "true";
}

std::vector<std::string> string_array(const std::string& text, const std::string& key) {
    const std::regex array_pattern("\\\"" + key + "\\\"\\s*:\\s*\\[([^\\]]*)\\]");
    std::smatch array_match;
    if (!std::regex_search(text, array_match, array_pattern)) {
        return {};
    }

    std::vector<std::string> values;
    const std::string body = array_match[1].str();
    const std::regex item_pattern("\\\"([^\\\"]*)\\\"");
    for (std::sregex_iterator iterator(body.begin(), body.end(), item_pattern), end;
         iterator != end;
         ++iterator) {
        values.push_back((*iterator)[1].str());
    }
    return values;
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
    if (text.empty()) {
        return config;
    }

    const auto base = path.parent_path().parent_path();
    const std::string input = string_value(text, "input");
    const std::string output = string_value(text, "output");
    config.input = resolve_path(base, input.empty() ? "." : input);
    config.output = output.empty() ? "" : resolve_path(base, output);
    config.ignore = string_array(text, "ignore");
    config.warnings_as_errors = bool_value(text, "warnings_as_errors");
    return config;
}

}  // namespace upd_checker
