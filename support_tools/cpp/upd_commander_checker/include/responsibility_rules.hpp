#pragma once

#include <string>
#include <vector>

#include "ignore_rules.hpp"
#include "models.hpp"

namespace upd_checker {

std::vector<Finding> check_responsibilities(
    const std::string& source_text,
    const std::string& path,
    const std::vector<IgnoreRule>& rules);

}  // namespace upd_checker
