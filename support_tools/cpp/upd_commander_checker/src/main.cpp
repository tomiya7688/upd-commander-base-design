#include <filesystem>
#include <iostream>
#include <map>
#include <string>
#include <utility>
#include <vector>

#include "baseline.hpp"
#include "cli_options.hpp"
#include "config.hpp"
#include "gate_policy.hpp"
#include "report_output.hpp"
#include "rule_selection.hpp"
#include "scanner.hpp"

int main(int argc, char* argv[]) {
    upd_checker::Config config;
    try {
        config = upd_checker::load_config(argv[0]);
    } catch (const upd_checker::ConfigError& error) {
        std::cout << "CONFIG ERROR: " << error.what() << '\n';
        return 2;
    }

    upd_checker::CliOptions options;
    try {
        options = upd_checker::parse_cli(argc, argv, config);
    } catch (const upd_checker::CliUsageError& error) {
        std::cout << "USAGE ERROR: " << error.what() << '\n';
        return 2;
    }

    if (!std::filesystem::exists(options.target)) {
        return upd_checker::finish_report(
            {"E UPD000 " + options.target + " missing"}, options.output, 2);
    }

    const auto scanned_findings =
        upd_checker::scan_path(
            options.target,
            options.ignores,
            config.upd301_max_inputs,
            config.flat_layer_min_files,
            config.flat_layer_min_direct_percent,
            config.model_group_min_items,
            config.model_group_min_occurrences);
    const auto selected_findings = upd_checker::filter_enabled_findings({
        scanned_findings,
        config.enabled_rules,
        config.enabled_rules_configured});
    const auto findings = upd_checker::apply_severity_overrides(
        selected_findings, options.severity_overrides);

    std::vector<upd_checker::BaselineEntry> baseline_findings;
    baseline_findings.reserve(findings.size());
    for (const auto& finding : findings) {
        baseline_findings.push_back({
            {}, finding.code, finding.path, finding.symbol, finding.context,
            finding.severity, finding.line, finding.message});
    }
    std::map<std::string, std::string> statuses;
    std::vector<upd_checker::BaselineEntry> resolved;
    try {
        if (options.write_baseline) {
            std::filesystem::path baseline_path(options.write_baseline_path);
            if (options.write_baseline_path.empty()) {
                const std::filesystem::path target_path(options.target);
                std::error_code error;
                const bool is_directory = std::filesystem::is_directory(target_path, error);
                const auto root = is_directory ? target_path : target_path.parent_path();
                baseline_path = root / ".upd-baseline.json";
            }
            upd_checker::write_baseline(baseline_path.string(), baseline_findings);
        } else if (!options.baseline_path.empty()) {
            const auto baseline = upd_checker::load_baseline(options.baseline_path);
            const auto comparison = upd_checker::compare_baseline(baseline_findings, baseline);
            for (const auto& entry : comparison.new_findings) statuses[entry.fingerprint] = "NEW";
            for (const auto& entry : comparison.existing) statuses[entry.fingerprint] = "EXISTING";
            resolved = comparison.resolved;
        }
    } catch (const std::exception& error) {
        return upd_checker::finish_report(
            {"BASELINE ERROR: " + std::string(error.what())}, options.output, 2);
    }

    int errors = 0;
    int warnings = 0;
    int attentions = 0;
    std::vector<std::string> lines;
    std::vector<upd_checker::Finding> gate_findings;
    for (const auto& finding : findings) {
        std::string level = "A ";
        if (finding.severity == "error") {
            level = "E ";
            ++errors;
        } else if (finding.severity == "warning") {
            level = "W ";
            ++warnings;
        } else {
            ++attentions;
        }
        std::string result_line =
            level + finding.code + " " + finding.path + ":" +
            std::to_string(finding.line) + " " + finding.message;
        const std::string reason =
            upd_checker::gate_exception_reason(finding, config.gate_exceptions);
        const std::string suffix = reason.empty()
            ? ""
            : " [gate exception: " + reason + "]";
        if (reason.empty()) {
            gate_findings.push_back(finding);
        }
        if (!options.baseline_path.empty()) {
            try {
                const auto fingerprint = upd_checker::finding_fingerprint(
                    finding.code, finding.path, finding.symbol, finding.context);
                result_line = statuses.at(fingerprint) + " " + result_line;
            } catch (const std::exception& error) {
                return upd_checker::finish_report(
                    {"BASELINE ERROR: " + std::string(error.what())}, options.output, 2);
            }
        }
        lines.push_back(std::move(result_line) + suffix);
    }
    for (const auto& finding : resolved) {
        const std::string level = finding.severity == "error" ? "E" :
            finding.severity == "warning" ? "W" : "A";
        lines.push_back("RESOLVED " + level + " " + finding.rule + " " + finding.path + ":" +
            (finding.line > 0 ? std::to_string(finding.line) : "?") + " " + finding.message);
    }

    std::vector<std::string> fail_on = options.fail_on;
    if (!options.fail_on_configured) {
        fail_on = {"error"};
        if (options.warnings_as_errors) {
            fail_on.push_back("warning");
        }
        if (options.attentions_as_errors) {
            fail_on.push_back("attention");
        }
    }
    if (upd_checker::should_fail(gate_findings, fail_on)) {
        lines.push_back(
            "FAIL e=" + std::to_string(errors) +
            " w=" + std::to_string(warnings) +
            " a=" + std::to_string(attentions));
        return upd_checker::finish_report(lines, options.output, 1);
    }
    if (warnings > 0 || attentions > 0) {
        lines.push_back(
            "OK w=" + std::to_string(warnings) +
            " a=" + std::to_string(attentions));
    } else {
        lines.push_back("OK");
    }
    return upd_checker::finish_report(lines, options.output, 0);
}
