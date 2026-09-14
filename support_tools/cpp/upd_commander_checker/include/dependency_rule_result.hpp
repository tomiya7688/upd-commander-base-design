#pragma once

#include <string>

namespace upd_checker {

struct DependencyRuleResult {
    std::string code;
    std::string message;
    std::string severity;
};

}  // namespace upd_checker
