#pragma once

#include <string>
#include <vector>

#include "strict_json.hpp"

namespace upd_checker {

std::string finding_fingerprint(
    const std::string& rule,
    const std::string& path,
    const std::string& symbol,
    const std::string& context);

struct BaselineEntry {
    std::string fingerprint;
    std::string rule;
    std::string path;
    std::string symbol;
    std::string context;
    std::string severity;
    int line = 0;
    std::string message;
};

struct Baseline {
    int schema_version = 1;
    int fingerprint_version = 1;
    std::vector<BaselineEntry> findings;
};

struct ClassifiedBaseline {
    std::vector<BaselineEntry> new_findings;
    std::vector<BaselineEntry> existing;
    std::vector<BaselineEntry> resolved;
};

Baseline build_baseline(const std::vector<BaselineEntry>& findings);
Baseline validate_baseline(const JsonValue& document);
Baseline load_baseline(const std::string& filename);
void write_baseline(const std::string& filename, const std::vector<BaselineEntry>& findings);
ClassifiedBaseline compare_baseline(
    const std::vector<BaselineEntry>& current,
    const Baseline& baseline);

}  // namespace upd_checker
