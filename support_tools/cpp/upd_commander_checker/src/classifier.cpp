#include "classifier.hpp"

#include <algorithm>
#include <cctype>
#include <sstream>
#include <unordered_set>
#include <vector>

namespace upd_checker {
namespace {

const std::unordered_set<std::string> kLayers = {"ui", "process", "data"};
const std::unordered_set<std::string> kRoles = {"commander", "messenger", "processing"};
const std::unordered_set<std::string> kAppRoots = {
    "app", "apps", "application", "applications", "feature", "features"};

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

std::string find_application(const std::vector<std::string>& parts) {
    for (std::size_t index = 0; index + 1 < parts.size(); ++index) {
        if (kAppRoots.count(parts[index]) != 0U) {
            return parts[index + 1];
        }
    }
    return {};
}

ModuleInfo classify(const std::string& value) {
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
    return classify(path);
}

ModuleInfo classify_include(const std::string& include_name) {
    return classify(include_name);
}

}  // namespace upd_checker
