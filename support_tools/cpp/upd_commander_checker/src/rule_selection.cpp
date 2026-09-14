#include "rule_selection.hpp"

#include <unordered_set>

namespace upd_checker {

const std::vector<std::string>& supported_rules() {
    static const std::vector<std::string> rules = {
        "UPD001", "UPD002",
        "UPD101", "UPD102", "UPD103",
        "UPD201", "UPD202", "UPD203",
        "UPD301", "UPD302", "UPD303",
        "UPD401", "UPD402", "UPD403", "UPD404"};
    return rules;
}

bool enabled_rules_are_valid(const std::vector<std::string>& rules) {
    const std::unordered_set<std::string> supported(
        supported_rules().begin(), supported_rules().end());
    for (const auto& rule : rules) {
        if (supported.find(rule) == supported.end()) {
            return false;
        }
    }
    return true;
}

std::vector<Finding> filter_enabled_findings(const RuleSelectionInput& input) {
    if (!input.enabled_rules_configured) {
        return input.findings;
    }
    const std::unordered_set<std::string> enabled(
        input.enabled_rules.begin(), input.enabled_rules.end());
    std::vector<Finding> filtered;
    for (const auto& finding : input.findings) {
        if (enabled.find(finding.code) != enabled.end()) {
            filtered.push_back(finding);
        }
    }
    return filtered;
}

}  // namespace upd_checker
