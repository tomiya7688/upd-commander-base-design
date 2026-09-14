#pragma once

#include <string>

namespace upd_checker {

struct Finding {
    std::string path;
    int line = 1;
    std::string code;
    std::string message;
    std::string severity = "error";
};

struct ModuleInfo {
    std::string path;
    std::string layer;
    std::string role;
    std::string application_id;
};

struct DependencyRuleResult {
    std::string code;
    std::string message;
    std::string severity;
};

}  // namespace upd_checker
