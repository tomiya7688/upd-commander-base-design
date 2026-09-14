#pragma once

#include <optional>

#include "models.hpp"

namespace upd_checker {

std::optional<DependencyRuleResult> dependency_result(
    const ModuleInfo& source,
    const ModuleInfo& target);

}  // namespace upd_checker
