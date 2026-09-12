#pragma once

#include <string>

#include "models.hpp"

namespace upd_checker {

ModuleInfo classify_path(const std::string& path);
ModuleInfo classify_include(const std::string& include_name);

}  // namespace upd_checker
