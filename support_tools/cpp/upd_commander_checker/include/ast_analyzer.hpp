#pragma once

#include <filesystem>
#include <string>
#include <vector>

#include "ignore_rules.hpp"
#include "models.hpp"

namespace upd_checker {

std::vector<Finding> analyze_cpp_ast(
    const std::filesystem::path& path,
    const std::filesystem::path& root,
    const std::string& relative,
    const std::vector<IgnoreRule>& rules);

}  // namespace upd_checker
