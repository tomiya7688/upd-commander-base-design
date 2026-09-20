#pragma once

#include <string>
#include <vector>

#include "models.hpp"

namespace upd_checker {

ModuleInfo classify_path(const std::string& path);
ModuleInfo classify_path(
    const std::string& path,
    const std::vector<std::string>& common_roots);
ModuleInfo classify_include(const std::string& include_name);
ModuleInfo classify_include(
    const std::string& include_name,
    const std::vector<std::string>& common_roots);

}  // namespace upd_checker
