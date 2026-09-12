#include <filesystem>
#include <fstream>
#include <iostream>
#include <string>
#include <vector>

#include "config.hpp"
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
    const upd_checker::Config config = upd_checker::load_config(argv[0]);
    std::string target = config.input;
    std::string output = config.output;
    std::vector<std::string> ignores = config.ignore;
    bool warnings_as_errors = config.warnings_as_errors;

    for (int index = 1; index < argc; ++index) {
        const std::string argument = argv[index];
        if (argument == "--ignore" && index + 1 < argc) {
            ignores.emplace_back(argv[++index]);
        } else if (argument == "--output" && index + 1 < argc) {
            output = argv[++index];
        } else if (argument == "--warnings-as-errors") {
            warnings_as_errors = true;
        } else {
            target = argument;
        }
    }

    if (!std::filesystem::exists(target)) {
        return finish({"E UPD000 " + target + " missing"}, output, 2);
    }

    const auto findings = upd_checker::scan_path(target, ignores);
    int errors = 0;
    int warnings = 0;
    std::vector<std::string> lines;
    for (const auto& finding : findings) {
        const bool warning = finding.severity == "warning";
        const std::string level = warning ? "W " : "E ";
        lines.push_back(
            level + finding.code + " " + finding.path + ":" +
            std::to_string(finding.line) + " " + finding.message);
        if (warning) {
            ++warnings;
        } else {
            ++errors;
        }
    }

    if (errors > 0 || (warnings_as_errors && warnings > 0)) {
        lines.push_back("FAIL e=" + std::to_string(errors) + " w=" + std::to_string(warnings));
        return finish(lines, output, 1);
    }
    lines.push_back(warnings > 0 ? "OK w=" + std::to_string(warnings) : "OK");
    return finish(lines, output, 0);
}
