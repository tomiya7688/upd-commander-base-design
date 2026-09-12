#pragma once

#include <string>
#include <vector>

namespace upd_checker {

struct IgnoreRule {
    std::string code;
    std::string pattern;
};

std::vector<IgnoreRule> load_ignore_rules(const std::string& root);
bool is_ignored(
    const std::string& path,
    const std::string& code,
    const std::string& line_text,
    const std::vector<IgnoreRule>& rules);
bool glob_match(const std::string& path, const std::string& pattern);

}  // namespace upd_checker
