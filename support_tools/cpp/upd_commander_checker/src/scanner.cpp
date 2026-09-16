#include "scanner.hpp"

#include <algorithm>
#include <filesystem>
#include <string>
#include <vector>

#include "ast_analyzer.hpp"
#include "data_type_location_rules.hpp"
#include "ignore_rules.hpp"
#include "source_file_walker.hpp"

namespace upd_checker {
namespace {

std::string relative_text(const std::filesystem::path& path, const std::filesystem::path& root) {
    std::error_code error;
    auto relative = std::filesystem::relative(path, root, error);
    return error ? path.generic_string() : relative.generic_string();
}

bool ignored_by_cli(const std::string& path, const std::vector<std::string>& patterns) {
    return std::any_of(patterns.begin(), patterns.end(), [&](const std::string& pattern) {
        return glob_match(path, pattern);
    });
}

}  // namespace

std::vector<Finding> scan_path(const std::string& target, const std::vector<std::string>& cli_ignore) {
    const std::filesystem::path target_path(target);
    std::error_code target_error;
    const bool target_is_directory = std::filesystem::is_directory(target_path, target_error);
    if (target_error) {
        return {Finding{
            target_path.generic_string(),
            1,
            "UPD001",
            "read failed: " + target_error.message(),
            "error",
        }};
    }
    const std::filesystem::path root = target_is_directory
        ? target_path
        : target_path.parent_path();

    std::vector<IgnoreRule> rules;
    try {
        rules = load_ignore_rules(root.string());
    } catch (const std::exception& error) {
        return {Finding{
            ".updcommanderignore",
            1,
            "UPD001",
            error.what(),
            "error",
        }};
    }

    std::vector<Finding> findings;
    const auto paths = collect_source_files(target_path, root, findings);
    std::vector<std::filesystem::path> included_paths;
    for (const auto& path : paths) {
        const std::string relative = relative_text(path, root);
        if (ignored_by_cli(relative, cli_ignore) || is_path_ignored(relative, rules)) {
            continue;
        }
        included_paths.push_back(path);
        auto current = analyze_cpp_ast(path, root, relative, rules);
        findings.insert(findings.end(), current.begin(), current.end());
    }

    auto location_findings = check_data_type_locations(included_paths, root, rules);
    findings.insert(findings.end(), location_findings.begin(), location_findings.end());

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
