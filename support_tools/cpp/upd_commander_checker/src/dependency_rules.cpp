#include "dependency_rules.hpp"

#include <algorithm>
#include <cctype>
#include <optional>
#include <sstream>
#include <string>
#include <unordered_set>

namespace upd_checker {
namespace {

const std::unordered_set<std::string> kBoundaryApiNames = {
    "contract", "contracts", "dto", "dtos", "message", "messages"};

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

DependencyRuleResult result(
    const std::string& code,
    const std::string& message,
    const std::string& severity) {
    return DependencyRuleResult{code, message, severity};
}

}  // namespace

std::optional<DependencyRuleResult> dependency_result(
    const ModuleInfo& source,
    const ModuleInfo& target) {
    if (!source.application_id.empty() && !target.application_id.empty() &&
        source.application_id != target.application_id && !is_boundary_api(target)) {
        return result("UPD102", "cross-application internal dependency", "error");
    }
    if (source.layer == "common" &&
        (target.layer == "ui" || target.layer == "process" || target.layer == "data")) {
        return result(
            "UPD101",
            "Common/Shared must not depend on layer-specific implementation",
            "error");
    }
    if (source.layer == "ui" && target.layer == "data") {
        return result("UPD101", "UI must not depend on Data", "error");
    }
    if (source.layer == "data" && target.layer == "ui") {
        return result("UPD101", "Data must not depend on UI", "error");
    }
    if (source.layer != "common" && target.layer != "common" &&
        source.role == "messenger" && target.role == "processing") {
        return result("UPD101", "Messenger must not depend on Processing", "error");
    }
    if (source.layer != "common" && target.layer != "common" &&
        source.role == "processing" && target.role == "processing") {
        return result("UPD101", "Processing must not depend on Processing", "error");
    }
    if (source.layer != "common" && target.layer != "common" &&
        source.role == "commander" && target.role == "processing" &&
        !source.layer.empty() && !target.layer.empty() && source.layer != target.layer) {
        return result(
            "UPD101",
            "Commander must not depend on Processing in another layer",
            "error");
    }
    if (source.layer == "data" && source.role == "commander" &&
        target.layer == "data" && target.role == "commander") {
        return result(
            "UPD103",
            "Data Commander should not communicate directly with another Data Commander",
            "warning");
    }
    return std::nullopt;
}

}  // namespace upd_checker
