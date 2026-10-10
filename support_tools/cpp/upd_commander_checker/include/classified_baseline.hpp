#pragma once

#include <vector>

#include "baseline_entry.hpp"

namespace upd_checker {

// {
// 責務: [ClassifiedBaseline: baseline比較結果を差分状態別に保持する]
// フィールド: [new_findings: 新規, existing: 継続中, resolved: 解消済み]
// }
struct ClassifiedBaseline {
    std::vector<BaselineEntry> new_findings;
    std::vector<BaselineEntry> existing;
    std::vector<BaselineEntry> resolved;
};

}  // namespace upd_checker
