#include "dependency_rules.hpp"

#include <algorithm>
#include <cctype>
#include <sstream>
#include <string>
#include <unordered_set>

namespace upd_checker {
namespace {

const std::unordered_set<std::string> kBoundaryApiNames = {
    "contract", "contracts", "dto", "dtos", "shared"};

bool has_boundary_component(std::string path) {
    std::transform(path.begin(), path.end(), path.begin(), [](unsigned char ch) {
        if (ch == '\\' || ch == '.' || ch == '-' || ch == '_') {
            return '/';
        }
        return static_cast<char>(std::tolower(ch));
    });

    std::stringstream stream(path);
    std::string part;
    while (std::getline(stream, part, '/')) {
        if (kBoundaryApiNames.count(part) != 0U) {
            return true;
        }
    }
    return false;
}

bool is_boundary_api(const ModuleInfo& target) {
    return target.role == "messenger" || has_boundary_component(target.path);
}

}  // namespace

std::string dependency_error(const ModuleInfo& source, const ModuleInfo& target) {
    if (!source.application_id.empty() && !target.application_id.empty() &&
        source.application_id != target.application_id && !is_boundary_api(target)) {
        return "cross-application internal dependency";
    }
    if (source.layer == "ui" && target.layer == "data") {
        return "UI must not depend on Data";
    }
    if (source.layer == "data" && target.layer == "ui") {
        return "Data must not depend on UI";
    }
    if (source.role == "messenger" && target.role == "processing") {
        return "Messenger must not depend on Processing";
    }
    if (source.role == "processing" && target.role == "processing") {
        return "Processing must not depend on Processing";
    }
    if (source.role == "commander" && target.role == "processing" &&
        !source.layer.empty() && !target.layer.empty() && source.layer != target.layer) {
        return "Commander must not depend on Processing in another layer";
    }
    return {};
}

std::string data_commander_warning(const ModuleInfo& source, const ModuleInfo& target) {
    if (source.layer == "data" && source.role == "commander" &&
        target.layer == "data" && target.role == "commander") {
        return "Data Commander should not communicate directly with another Data Commander";
    }
    return {};
}

}  // namespace upd_checker
