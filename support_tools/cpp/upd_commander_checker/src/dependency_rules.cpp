#include "dependency_rules.hpp"

#include <algorithm>
#include <cctype>
#include <string>

namespace upd_checker {
namespace {

bool is_boundary_api(const ModuleInfo& target) {
    if (target.role == "messenger") {
        return true;
    }
    std::string path = target.path;
    std::transform(path.begin(), path.end(), path.begin(), [](unsigned char ch) {
        return static_cast<char>(std::tolower(ch));
    });
    return path.find("/contract") != std::string::npos ||
           path.find("/contracts") != std::string::npos ||
           path.find("/dto") != std::string::npos ||
           path.find("/dtos") != std::string::npos ||
           path.find("/shared") != std::string::npos;
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

}  // namespace upd_checker
