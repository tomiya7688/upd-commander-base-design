#pragma once

#include <filesystem>
#include <vector>

#include "ignore_rules.hpp"
#include "models.hpp"

namespace upd_checker {

std::vector<Finding> check_data_type_locations(
    const std::vector<std::filesystem::path>& paths,
    const std::filesystem::path& root,
    const std::vector<IgnoreRule>& rules);

}  // namespace upd_checker
