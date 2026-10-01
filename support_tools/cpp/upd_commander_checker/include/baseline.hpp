#pragma once

#include <string>
#include <vector>

#include "baseline_document.hpp"
#include "classified_baseline.hpp"
#include "baseline_entry.hpp"
#include "strict_json.hpp"

namespace upd_checker {

std::string finding_fingerprint(
    const std::string& rule,
    const std::string& path,
    const std::string& symbol,
    const std::string& context);

Baseline build_baseline(const std::vector<BaselineEntry>& findings);
Baseline validate_baseline(const JsonValue& document);
Baseline load_baseline(const std::string& filename);
void write_baseline(const std::string& filename, const std::vector<BaselineEntry>& findings);
ClassifiedBaseline compare_baseline(
    const std::vector<BaselineEntry>& current,
    const Baseline& baseline);

}  // namespace upd_checker
