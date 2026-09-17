#include "executable_path.hpp"

#include <cstdlib>
#include <filesystem>
#include <string>
#include <vector>

namespace upd_checker {
namespace {

std::filesystem::path absolute_path(const std::filesystem::path& path) {
    std::error_code error;
    const auto absolute = std::filesystem::absolute(path, error);
    return error ? path : absolute;
}

std::vector<std::string> split_environment(const std::string& value, char separator) {
    std::vector<std::string> parts;
    std::size_t start = 0;
    while (start <= value.size()) {
        const auto end = value.find(separator, start);
        parts.push_back(value.substr(start, end == std::string::npos ? end : end - start));
        if (end == std::string::npos) {
            break;
        }
        start = end + 1;
    }
    return parts;
}

std::string unquote(std::string value) {
    if (value.size() >= 2 && value.front() == '"' && value.back() == '"') {
        return value.substr(1, value.size() - 2);
    }
    return value;
}

std::vector<std::string> executable_suffixes(const std::filesystem::path& invoked) {
#ifdef _WIN32
    if (invoked.has_extension()) {
        return {""};
    }
    const char* raw = std::getenv("PATHEXT");
    const std::string value = raw == nullptr ? ".COM;.EXE;.BAT;.CMD" : raw;
    auto suffixes = split_environment(value, ';');
    suffixes.insert(suffixes.begin(), "");
    return suffixes;
#else
    (void)invoked;
    return {""};
#endif
}

std::filesystem::path path_candidate(
    const std::filesystem::path& directory,
    const std::filesystem::path& invoked,
    const std::string& suffix) {
    const auto name = invoked.filename().string() + suffix;
    return directory / name;
}

char environment_path_separator() {
#ifdef _WIN32
    return ';';
#else
    return ':';
#endif
}

}  // namespace

std::filesystem::path resolve_executable_path(const std::string& invoked_path) {
    const std::filesystem::path invoked(invoked_path);
    if (invoked.empty()) {
        return {};
    }
    if (invoked.is_absolute() || invoked.has_parent_path()) {
        return absolute_path(invoked);
    }

    const char* raw_path = std::getenv("PATH");
    if (raw_path != nullptr) {
        const auto suffixes = executable_suffixes(invoked);
        for (const auto& raw_directory : split_environment(raw_path, environment_path_separator())) {
            const std::filesystem::path directory = raw_directory.empty()
                ? std::filesystem::current_path()
                : std::filesystem::path(unquote(raw_directory));
            for (const auto& suffix : suffixes) {
                const auto candidate = path_candidate(directory, invoked, suffix);
                std::error_code error;
                if (std::filesystem::is_regular_file(candidate, error) && !error) {
                    return absolute_path(candidate);
                }
            }
        }
    }
    return absolute_path(invoked);
}

}  // namespace upd_checker
