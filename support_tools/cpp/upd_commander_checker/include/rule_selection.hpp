#pragma once

#include <string>
#include <vector>

#include "finding.hpp"

namespace upd_checker {

const std::vector<std::string>& supported_rules();
bool enabled_rules_are_valid(const std::vector<std::string>& rules);
struct RuleSelectionInput {
    const std::vector<Finding>& findings;
    const std::vector<std::string>& enabled_rules;
    bool enabled_rules_configured;
};

std::vector<Finding> filter_enabled_findings(const RuleSelectionInput& input);

}  // namespace upd_checker
