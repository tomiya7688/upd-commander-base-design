#include <filesystem>
#include <iostream>
#include <string>
#include <vector>

#include "scanner.hpp"

int main(int argc, char* argv[]) {
    std::string target = ".";
    std::vector<std::string> ignores;
    bool warnings_as_errors = false;

    for (int index = 1; index < argc; ++index) {
        const std::string argument = argv[index];
        if (argument == "--ignore" && index + 1 < argc) {
            ignores.emplace_back(argv[++index]);
        } else if (argument == "--warnings-as-errors") {
            warnings_as_errors = true;
        } else {
            target = argument;
        }
    }

    if (!std::filesystem::exists(target)) {
        std::cout << "E UPD000 " << target << " missing\n";
        return 2;
    }

    const auto findings = upd_checker::scan_path(target, ignores);
    int errors = 0;
    int warnings = 0;
    for (const auto& finding : findings) {
        const bool warning = finding.severity == "warning";
        std::cout << (warning ? "W " : "E ") << finding.code << ' '
                  << finding.path << ':' << finding.line << ' ' << finding.message << '\n';
        if (warning) {
            ++warnings;
        } else {
            ++errors;
        }
    }

    if (errors > 0 || (warnings_as_errors && warnings > 0)) {
        std::cout << "FAIL e=" << errors << " w=" << warnings << '\n';
        return 1;
    }
    if (warnings > 0) {
        std::cout << "OK w=" << warnings << '\n';
    } else {
        std::cout << "OK\n";
    }
    return 0;
}
