#pragma once

#include <map>
#include <string>
#include <vector>

#include "finding.hpp"

namespace upd_checker {

bool normalize_fail_on(
    const std::vector<std::string>& values,
    std::vector<std::string>* normalized);
bool parse_fail_on_argument(
    const std::string& value,
    std::vector<std::string>* normalized);
bool normalize_severity_overrides(
    const std::map<std::string, std::string>& values,
    std::map<std::string, std::string>* normalized);
bool parse_severity_override_argument(
    const std::string& value,
    std::string* rule,
    std::string* severity);
std::vector<Finding> apply_severity_overrides(
    const std::vector<Finding>& findings,
    const std::map<std::string, std::string>& overrides);
bool should_fail(
    const std::vector<Finding>& findings,
    const std::vector<std::string>& fail_on);

}  // namespace upd_checker
