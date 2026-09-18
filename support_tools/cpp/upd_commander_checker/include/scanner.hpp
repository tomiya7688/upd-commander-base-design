#pragma once

#include <string>
#include <vector>

#include "models.hpp"

namespace upd_checker {

std::vector<Finding> scan_path(
    const std::string& target,
    const std::vector<std::string>& cli_ignore,
    int upd301_max_inputs = 2,
    int flat_layer_min_files = 12,
    int flat_layer_min_direct_percent = 80);

}  // namespace upd_checker
