#include "scanner.hpp"

#include <algorithm>
#include <filesystem>
#include <fstream>
#include <regex>
#include <string>
#include <vector>

#include "classifier.hpp"
#include "dependency_rules.hpp"
#include "ignore_rules.hpp"

namespace upd_checker {
namespace {

bool is_source_file(const std::filesystem::path& path) {
    const auto ext = path.extension().string();
    return ext == ".cpp" || ext == ".cc" || ext == ".cxx" ||
           ext == ".hpp" || ext == ".h" || ext == ".hh" || ext == ".hxx";
}

std::string relative_text(
    const std::filesystem::path& path,
    const std::filesystem::path& root) {
    std::error_code error;
    auto relative = std::filesystem::relative(path, root, error);
    if (error) {
        relative = path;
    }
    return relative.generic_string();
}

bool cli_path_ignored(
    const std::string& path,
    const std::vector<std::string>& patterns) {
    for (const auto& pattern : patterns) {
        if (glob_match(path, pattern)) {
            return true;
        }
    }
    return false;
}

void add_finding(
    std::vector<Finding>& findings,
    const std::string& path,
    int line,
    const std::string& code,
    const std::string& message,
    const std::string& severity,
    const std::string& line_text,
    const std::vector<IgnoreRule>& rules) {
    if (!is_ignored(path, code, line_text, rules)) {
        findings.push_back(Finding{path, line, code, message, severity});
    }
}

std::vector<Finding> scan_file(
    const std::filesystem::path& path,
    const std::string& relative,
    const std::vector<IgnoreRule>& rules) {
    std::ifstream file(path);
    if (!file) {
        return {Finding{relative, 1, "UPD001", "read failed", "error"}};
    }

    const ModuleInfo source = classify_path(relative);
    const std::regex include_pattern(R"(^\s*#\s*include\s*[<\"]([^>\"]+)[>\"])");
    const std::regex loop_pattern(R"(\b(for|while)\s*\()");
    const std::regex calc_pattern(R"([^+*/%<>=!-][+*/%][^=+*/])");
    const std::regex io_pattern(
        R"((std::(ifstream|ofstream|fstream)|fopen\s*\(|sqlite|curl_|json::))");

    std::vector<Finding> findings;
    std::string line_text;
    int line_number = 0;
    while (std::getline(file, line_text)) {
        ++line_number;
        std::smatch match;
        if (std::regex_search(line_text, match, include_pattern)) {
            const ModuleInfo target = classify_include(match[1].str());
            const std::string message = dependency_error(source, target);
            if (!message.empty()) {
                const bool cross_application =
                    !source.application_id.empty() && !target.application_id.empty() &&
                    source.application_id != target.application_id;
                add_finding(
                    findings,
                    relative,
                    line_number,
                    cross_application ? "UPD102" : "UPD101",
                    message,
                    "error",
                    line_text,
                    rules);
            }
        }

        if (source.role != "commander") {
            continue;
        }
        if (std::regex_search(line_text, loop_pattern)) {
            add_finding(findings, relative, line_number, "UPD201", "Commander loop", "warning", line_text, rules);
        }
        if (std::regex_search(line_text, calc_pattern)) {
            add_finding(findings, relative, line_number, "UPD202", "Commander calculation", "warning", line_text, rules);
        }
        if (std::regex_search(line_text, io_pattern)) {
            add_finding(findings, relative, line_number, "UPD203", "Commander direct I/O/API call", "error", line_text, rules);
        }
    }
    return findings;
}

}  // namespace

std::vector<Finding> scan_path(
    const std::string& target,
    const std::vector<std::string>& cli_ignore) {
    const std::filesystem::path target_path(target);
    const std::filesystem::path root =
        std::filesystem::is_directory(target_path) ? target_path : target_path.parent_path();
    const auto rules = load_ignore_rules(root.string());

    std::vector<Finding> findings;
    auto scan_one = [&](const std::filesystem::path& path) {
        if (!is_source_file(path)) {
            return;
        }
        const std::string relative = relative_text(path, root);
        if (cli_path_ignored(relative, cli_ignore)) {
            return;
        }
        auto file_findings = scan_file(path, relative, rules);
        findings.insert(findings.end(), file_findings.begin(), file_findings.end());
    };

    if (std::filesystem::is_regular_file(target_path)) {
        scan_one(target_path);
    } else if (std::filesystem::is_directory(target_path)) {
        for (const auto& entry : std::filesystem::recursive_directory_iterator(target_path)) {
            if (entry.is_regular_file()) {
                scan_one(entry.path());
            }
        }
    }

    std::sort(findings.begin(), findings.end(), [](const Finding& left, const Finding& right) {
        if (left.path != right.path) {
            return left.path < right.path;
        }
        if (left.line != right.line) {
            return left.line < right.line;
        }
        return left.code < right.code;
    });
    return findings;
}

}  // namespace upd_checker
