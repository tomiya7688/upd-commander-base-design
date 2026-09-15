#include "report_output.hpp"

#include <filesystem>
#include <fstream>
#include <iostream>

namespace upd_checker {
namespace {

int output_error(const std::string& output) {
    std::cout << "I/O ERROR: failed to write output: " << output << '\n';
    return 2;
}

}  // namespace

int finish_report(
    const std::vector<std::string>& lines,
    const std::string& output,
    int exit_code) {
    for (const auto& line : lines) {
        std::cout << line << '\n';
    }
    if (output.empty()) {
        return exit_code;
    }

    try {
        const std::filesystem::path path(output);
        if (!path.parent_path().empty()) {
            std::filesystem::create_directories(path.parent_path());
        }
        std::ofstream file(path);
        if (!file.is_open()) {
            return output_error(output);
        }
        for (const auto& line : lines) {
            file << line << '\n';
            if (!file) {
                return output_error(output);
            }
        }
        file.close();
        if (!file) {
            return output_error(output);
        }
    } catch (const std::filesystem::filesystem_error&) {
        return output_error(output);
    }
    return exit_code;
}

}  // namespace upd_checker
