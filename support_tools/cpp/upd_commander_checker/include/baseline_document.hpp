#pragma once

#include <vector>

#include "baseline_entry.hpp"

namespace upd_checker {

struct Baseline {
    int schema_version = 1;
    int fingerprint_version = 1;
    std::vector<BaselineEntry> findings;
};

}  // namespace upd_checker
