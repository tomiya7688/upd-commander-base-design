#include "scanner.hpp"

#include <algorithm>
#include <filesystem>
#include <string>
#include <vector>

#include "ast_analyzer.hpp"
#include "data_type_location_rules.hpp"
#include "ignore_rules.hpp"

namespace upd_checker {
namespace {

bool is_source_file(const std::filesystem::path& path) {
    const auto ext = path.extension().string();
    return ext == ".cpp" || ext == ".cc" || ext == ".cxx" ||
           ext == ".hpp" || ext == ".h" || ext == ".hh" || ext == ".hxx";
}

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
    const std::filesystem::path root = std::filesystem::is_directory(target_path)
        ? target_path
        : target_path.parent_path();
    const auto rules = load_ignore_rules(root.string());
    std::vector<Finding> findings;
    std::vector<std::filesystem::path> paths;

    auto scan_one = [&](const std::filesystem::path& path) {
        if (!is_source_file(path)) {
            return;
        }
        const std::string relative = relative_text(path, root);
        if (ignored_by_cli(relative, cli_ignore) || is_path_ignored(relative, rules)) {
            return;
        }
        paths.push_back(path);
        auto current = analyze_cpp_ast(path, root, relative, rules);
        findings.insert(findings.end(), current.begin(), current.end());
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

    auto location_findings = check_data_type_locations(paths, root, rules);
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
