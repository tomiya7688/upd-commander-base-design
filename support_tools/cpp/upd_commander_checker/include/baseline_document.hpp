#pragma once

#include <vector>

#include "baseline_entry.hpp"

namespace upd_checker {

// {
// 責務: [Baseline: version付きbaseline文書を表す]
// フィールド: [schema_version/fingerprint_version: 契約version, findings: entry一覧]
// }
struct Baseline {
    int schema_version = 1;
    int fingerprint_version = 1;
    std::vector<BaselineEntry> findings;
};

}  // namespace upd_checker
