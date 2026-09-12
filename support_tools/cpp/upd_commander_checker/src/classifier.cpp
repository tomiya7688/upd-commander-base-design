#include "classifier.hpp"

#include <algorithm>
#include <cctype>
#include <filesystem>
#include <sstream>
#include <unordered_set>
#include <vector>

namespace upd_checker {
namespace {

const std::unordered_set<std::string> kLayers = {"ui", "process", "data"};
const std::unordered_set<std::string> kRoles = {"commander", "messenger", "processing"};
const std::unordered_set<std::string> kAppRoots = {
    "app", "apps", "application", "applications", "feature", "features"};

std::string lower(std::string value) {
    std::transform(value.begin(), value.end(), value.begin(), [](unsigned char ch) {
        return static_cast<char>(std::tolower(ch));
    });
    return value;
}

std::vector<std::string> split_parts(std::string value) {
    std::transform(value.begin(), value.end(), value.begin(), [](unsigned char ch) {
        if (ch == '\\' || ch == '.' || ch == '-' || ch == '_') {
            return '/';
        }
        return static_cast<char>(std::tolower(ch));
    });

    std::vector<std::string> parts;
    std::stringstream stream(value);
    std::string item;
    while (std::getline(stream, item, '/')) {
        if (!item.empty()) {
            parts.push_back(item);
        }
    }
    return parts;
}

std::vector<std::string> path_directories(const std::string& value) {
    std::filesystem::path path(value);
    std::vector<std::string> parts;
    for (const auto& part : path.parent_path()) {
        const std::string text = lower(part.string());
        if (!text.empty() && text != ".") {
            parts.push_back(text);
        }
    }
    return parts;
}

std::string find_name(
    const std::vector<std::string>& parts,
    const std::unordered_set<std::string>& candidates) {
    for (const auto& part : parts) {
        if (candidates.count(part) != 0U) {
            return part;
        }
    }
    return {};
}

std::string find_role(const std::vector<std::string>& parts) {
    for (const auto& part : parts) {
        if (kRoles.count(part) != 0U) {
            return part;
        }
        if (part.size() >= 9 && part.rfind("commander") == part.size() - 9) {
            return "commander";
        }
        if (part.size() >= 9 && part.rfind("messenger") == part.size() - 9) {
            return "messenger";
        }
        if (part.size() >= 10 && part.rfind("processing") == part.size() - 10) {
            return "processing";
        }
    }
    return {};
}

std::string find_path_role(const std::vector<std::string>& directories, const std::string& stem) {
    const std::string directory_role = find_name(directories, kRoles);
    if (!directory_role.empty()) {
        return directory_role;
    }
    for (const auto& role : kRoles) {
        if (stem == role ||
            (stem.size() > role.size() && stem.rfind("_" + role) == stem.size() - role.size() - 1)) {
            return role;
        }
    }
    return {};
}

std::string find_application(const std::vector<std::string>& parts) {
    for (std::size_t index = 0; index + 1 < parts.size(); ++index) {
        if (kAppRoots.count(parts[index]) != 0U) {
            return parts[index + 1];
        }
    }
    return {};
}

ModuleInfo classify_reference(const std::string& value) {
    const auto parts = split_parts(value);
    return ModuleInfo{
        value,
        find_name(parts, kLayers),
        find_role(parts),
        find_application(parts),
    };
}

}  // namespace

ModuleInfo classify_path(const std::string& path) {
    const auto directories = path_directories(path);
    const std::string stem = lower(std::filesystem::path(path).stem().string());
    return ModuleInfo{
        path,
        find_name(directories, kLayers),
        find_path_role(directories, stem),
        find_application(directories),
    };
}

ModuleInfo classify_include(const std::string& include_name) {
    return classify_reference(include_name);
}

}  // namespace upd_checker
