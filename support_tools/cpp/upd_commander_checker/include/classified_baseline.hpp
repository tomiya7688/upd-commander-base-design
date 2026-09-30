#pragma once

#include <vector>

#include "baseline_entry.hpp"

namespace upd_checker {

struct ClassifiedBaseline {
    std::vector<BaselineEntry> new_findings;
    std::vector<BaselineEntry> existing;
    std::vector<BaselineEntry> resolved;
};

}  // namespace upd_checker
