#include <filesystem>
#include <fstream>
#include <iostream>
#include <string>
#include <vector>

#include "config.hpp"
#include "rule_selection.hpp"
#include "scanner.hpp"

namespace {

void write_output(const std::string& path, const std::vector<std::string>& lines) {
    if (path.empty()) {
        return;
    }
    const std::filesystem::path output(path);
    if (!output.parent_path().empty()) {
        std::filesystem::create_directories(output.parent_path());
    }
    std::ofstream file(output);
    for (const auto& line : lines) {
        file << line << '\n';
    }
}

int finish(const std::vector<std::string>& lines, const std::string& output, int code) {
    for (const auto& line : lines) {
        std::cout << line << '\n';
    }
    write_output(output, lines);
    return code;
}

}  // namespace

int main(int argc, char* argv[]) {
    upd_checker::Config config;
    try {
        config = upd_checker::load_config(argv[0]);
    } catch (const upd_checker::ConfigError& error) {
        std::cout << "CONFIG ERROR: " << error.what() << '\n';
        return 2;
    }

    std::string target = config.input;
    std::string output = config.output;
    std::vector<std::string> ignores = config.ignore;
    bool warnings_as_errors = config.warnings_as_errors;
    bool attentions_as_errors = false;

    for (int index = 1; index < argc; ++index) {
        const std::string argument = argv[index];
        if (argument == "--ignore" && index + 1 < argc) {
            ignores.emplace_back(argv[++index]);
        } else if (argument == "--output" && index + 1 < argc) {
            output = argv[++index];
        } else if (argument == "--warnings-as-errors") {
            warnings_as_errors = true;
        } else if (argument == "--attentions-as-errors") {
            attentions_as_errors = true;
        } else {
            target = argument;
        }
    }

    if (!std::filesystem::exists(target)) {
        return finish({"E UPD000 " + target + " missing"}, output, 2);
    }

    const auto scanned_findings = upd_checker::scan_path(target, ignores);
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
        (warnings_as_errors && warnings > 0) ||
        (attentions_as_errors && attentions > 0)) {
        lines.push_back(
            "FAIL e=" + std::to_string(errors) +
            " w=" + std::to_string(warnings) +
            " a=" + std::to_string(attentions));
        return finish(lines, output, 1);
    }
    if (warnings > 0 || attentions > 0) {
        lines.push_back(
            "OK w=" + std::to_string(warnings) +
            " a=" + std::to_string(attentions));
    } else {
        lines.push_back("OK");
    }
    return finish(lines, output, 0);
}
