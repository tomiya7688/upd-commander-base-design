#include <filesystem>
#include <iostream>
#include <string>
#include <vector>

#include "cli_options.hpp"
#include "config.hpp"
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
            config.model_group_min_occurrences,
            config.common_roots);
    const auto findings = upd_checker::filter_enabled_findings({
        scanned_findings,
        config.enabled_rules,
        config.enabled_rules_configured});
    int errors = 0;
    int warnings = 0;
    int attentions = 0;
    std::vector<std::string> lines;
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
        lines.push_back(
            level + finding.code + " " + finding.path + ":" +
            std::to_string(finding.line) + " " + finding.message);
    }

    if (errors > 0 ||
        (options.warnings_as_errors && warnings > 0) ||
        (options.attentions_as_errors && attentions > 0)) {
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
