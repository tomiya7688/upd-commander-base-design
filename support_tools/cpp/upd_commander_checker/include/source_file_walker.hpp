#pragma once

#include <filesystem>
#include <vector>

#include "finding.hpp"

namespace upd_checker {

std::vector<std::filesystem::path> collect_source_files(
    const std::filesystem::path& target,
    const std::filesystem::path& root,
    std::vector<Finding>& findings);

}  // namespace upd_checker
