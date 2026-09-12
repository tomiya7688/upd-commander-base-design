#pragma once

#include <string>

#include "models.hpp"

namespace upd_checker {

std::string dependency_error(const ModuleInfo& source, const ModuleInfo& target);

}  // namespace upd_checker
