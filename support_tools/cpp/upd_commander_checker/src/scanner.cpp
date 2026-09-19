#include "scanner.hpp"

#include <algorithm>
#include <cctype>
#include <filesystem>
#include <string>
#include <unordered_map>
#include <vector>

#include "ast_analyzer.hpp"
#include "data_type_location_rules.hpp"
#include "ignore_rules.hpp"
#include "model_attention_rules.hpp"
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

std::string lower_name(const std::filesystem::path& path) {
    std::string value = path.filename().string();
    std::transform(value.begin(), value.end(), value.begin(), [](unsigned char ch) {
        return static_cast<char>(std::tolower(ch));
    });
    return value;
}

bool is_application_marker(const std::string& value) {
    return value == "app" || value == "apps" || value == "application" ||
           value == "applications" || value == "feature" || value == "features";
}

bool is_layer(const std::string& value) {
    return value == "ui" || value == "process" || value == "data";
}

bool is_flat_layer_excluded(const std::string& value) {
    return value == "generated" || value == "third_party" || value == "vendor" ||
           value == "external" || value == "build";
}

std::vector<std::string> relative_parts(
    const std::filesystem::path& path,
    const std::filesystem::path& root) {
    const auto relative = std::filesystem::path(relative_text(path, root));
    std::vector<std::string> parts;
    for (const auto& part : relative) {
        parts.push_back(part.string());
    }
    return parts;
}

int flat_layer_root_index(const std::vector<std::string>& parts) {
    int scope_start = 0;
    for (std::size_t index = 0; index + 2 < parts.size(); ++index) {
        if (is_application_marker(lower_name(parts[index]))) {
            scope_start = static_cast<int>(index + 2);
        }
    }

    int layer_index = -1;
    for (int index = scope_start; index + 1 < static_cast<int>(parts.size()); ++index) {
        if (is_layer(lower_name(parts[static_cast<std::size_t>(index)]))) {
            layer_index = index;
        }
    }
    return layer_index;
}

std::vector<Finding> check_flat_layers(
    const std::vector<std::filesystem::path>& paths,
    const std::filesystem::path& root,
    const std::vector<IgnoreRule>& rules,
    int min_files,
    int min_direct_percent) {
    struct Count {
        int total = 0;
        int direct = 0;
    };
    std::unordered_map<std::string, Count> counts;

    for (const auto& path : paths) {
        const auto parts = relative_parts(path, root);
        bool excluded = false;
        for (std::size_t index = 0; index + 1 < parts.size(); ++index) {
            if (is_flat_layer_excluded(lower_name(parts[index]))) {
                excluded = true;
                break;
            }
        }
        if (excluded) {
            continue;
        }

        const int layer_index = flat_layer_root_index(parts);
        if (layer_index < 0) {
            continue;
        }

        std::filesystem::path layer_root;
        for (int index = 0; index <= layer_index; ++index) {
            layer_root /= parts[static_cast<std::size_t>(index)];
        }
        const std::string layer_root_text = layer_root.generic_string();
        auto& count = counts[layer_root_text];
        ++count.total;
        if (parts.size() == static_cast<std::size_t>(layer_index + 2)) {
            ++count.direct;
        }
    }

    std::vector<Finding> findings;
    for (const auto& entry : counts) {
        const auto& layer_root = entry.first;
        const auto& count = entry.second;
        if (count.total < min_files ||
            count.direct * 100 < count.total * min_direct_percent ||
            is_ignored(layer_root, "UPD405", "", rules)) {
            continue;
        }
        findings.push_back(Finding{
            layer_root,
            1,
            "UPD405",
            "large flat layer reduces navigability; consider grouping related responsibilities",
            "attention",
        });
    }
    return findings;
}

std::filesystem::path single_file_context_root(const std::filesystem::path& file) {
    const auto directory = file.parent_path();
    std::filesystem::path application_root;
    for (auto current = directory; !current.empty(); current = current.parent_path()) {
        if (is_application_marker(lower_name(current))) {
            application_root = current.parent_path();
        }
        const auto parent = current.parent_path();
        if (parent == current) {
            break;
        }
    }
    if (!application_root.empty()) {
        return application_root;
    }

    for (auto current = directory; !current.empty(); current = current.parent_path()) {
        if (is_layer(lower_name(current))) {
            return current.parent_path();
        }
        const auto parent = current.parent_path();
        if (parent == current) {
            break;
        }
    }
    return directory;
}

}  // namespace

std::vector<Finding> scan_path(
    const std::string& target,
    const std::vector<std::string>& cli_ignore,
    int upd301_max_inputs,
    int flat_layer_min_files,
    int flat_layer_min_direct_percent,
    int model_group_min_items,
    int model_group_min_occurrences) {
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
        : single_file_context_root(target_path);

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
    std::vector<ModelGroupOccurrence> model_occurrences;
    for (const auto& path : paths) {
        const std::string relative = relative_text(path, root);
        if (ignored_by_cli(relative, cli_ignore) || is_path_ignored(relative, rules)) {
            continue;
        }
        included_paths.push_back(path);
        auto current = analyze_cpp_ast(path, root, relative, rules, upd301_max_inputs);
        findings.insert(findings.end(), current.begin(), current.end());
        auto occurrences = collect_cpp_model_group_occurrences(
            path,
            root,
            relative,
            rules,
            model_group_min_items);
        model_occurrences.insert(
            model_occurrences.end(),
            occurrences.begin(),
            occurrences.end());
    }

    auto location_findings = check_data_type_locations(included_paths, root, rules);
    findings.insert(findings.end(), location_findings.begin(), location_findings.end());

    auto model_findings =
        model_attention_findings(model_occurrences, model_group_min_occurrences);
    findings.insert(findings.end(), model_findings.begin(), model_findings.end());

    if (target_is_directory) {
        auto flat_findings = check_flat_layers(
            included_paths,
            root,
            rules,
            flat_layer_min_files,
            flat_layer_min_direct_percent);
        findings.insert(findings.end(), flat_findings.begin(), flat_findings.end());
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
