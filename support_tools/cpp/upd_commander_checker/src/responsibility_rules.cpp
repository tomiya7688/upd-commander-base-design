#include "responsibility_rules.hpp"

#include <regex>
#include <sstream>
#include <string>
#include <vector>

namespace upd_checker {
namespace {

constexpr int kMaxResponsibilityLines = 250;

bool is_blank(const std::string& line) {
    return line.find_first_not_of(" \t\r\n") == std::string::npos;
}

}  // namespace

std::vector<Finding> check_responsibilities(
    const std::string& source_text,
    const std::string& path,
    const std::vector<IgnoreRule>& rules) {
    std::vector<Finding> findings;
    std::istringstream stream(source_text);
    std::string line;
    int line_number = 0;
    int non_blank_lines = 0;
    int major_class_count = 0;
    int second_class_line = 0;
    const std::regex class_pattern(R"(^\s*class\s+[A-Za-z_][A-Za-z0-9_]*\s*(?:[:{]))");

    while (std::getline(stream, line)) {
        ++line_number;
        if (!is_blank(line)) {
            ++non_blank_lines;
        }
        if (std::regex_search(line, class_pattern)) {
            ++major_class_count;
            if (major_class_count == 2) {
                second_class_line = line_number;
            }
        }
    }

    if (non_blank_lines > kMaxResponsibilityLines &&
        !is_ignored(path, "UPD401", "", rules)) {
        findings.push_back(Finding{
            path,
            1,
            "UPD401",
            "file/module is too large for one responsibility",
            "warning",
        });
    }

    if (major_class_count > 1 &&
        !is_ignored(path, "UPD402", "", rules)) {
        findings.push_back(Finding{
            path,
            second_class_line,
            "UPD402",
            "file contains multiple major classes",
            "warning",
        });
    }

    return findings;
}

}  // namespace upd_checker
