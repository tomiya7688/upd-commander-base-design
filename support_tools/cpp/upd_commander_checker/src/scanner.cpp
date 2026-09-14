#include "scanner.hpp"

#include <algorithm>
#include <filesystem>
#include <fstream>
#include <regex>
#include <sstream>
#include <string>
#include <vector>

#include "classifier.hpp"
#include "dependency_rules.hpp"
#include "ignore_rules.hpp"
#include "responsibility_rules.hpp"

namespace upd_checker {
namespace {

constexpr int kMinReducibleLines = 10;
constexpr double kMinReductionRatio = 0.20;

bool is_source_file(const std::filesystem::path& path) {
    const auto ext = path.extension().string();
    return ext == ".cpp" || ext == ".cc" || ext == ".cxx" ||
           ext == ".hpp" || ext == ".h" || ext == ".hh" || ext == ".hxx";
}

bool is_component_role(const std::string& role) {
    return role == "commander" || role == "messenger" || role == "processing";
}

int count_parameters(const std::string& parameters) {
    std::string text = parameters;
    text.erase(std::remove_if(text.begin(), text.end(), [](unsigned char ch) {
        return ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n';
    }), text.end());
    if (text.empty() || text == "void") {
        return 0;
    }
    return 1 + static_cast<int>(std::count(text.begin(), text.end(), ','));
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
    const std::regex io_pattern(R"((std::(ifstream|ofstream|fstream)|fopen\s*\(|sqlite|curl_|json::))");
    const std::regex method_pattern(R"(^\s*(?:virtual\s+|static\s+|inline\s+|constexpr\s+|consteval\s+|friend\s+)*[A-Za-z_][A-Za-z0-9_:<>,*&\s]*\s+[A-Za-z_~][A-Za-z0-9_:~]*\s*\(([^()]*)\)\s*(?:const\s*)?(?:override\s*)?(?:final\s*)?(?:\{|;)$)");
    const std::regex tuple_return_pattern(R"(^\s*std::(tuple|pair)\s*<)");

    std::vector<Finding> findings;
    std::string line_text;
    int line_number = 0;
    int effective_lines = 0;
    int reducible_lines = 0;
    int first_offending_line = 1;
    std::string first_offending_text;
    while (std::getline(file, line_text)) {
        ++line_number;
        if (line_text.find_first_not_of(" \t\r\n") != std::string::npos) {
            ++effective_lines;
        }
        std::smatch match;
        if (std::regex_search(line_text, match, include_pattern)) {
            const ModuleInfo target = classify_include(match[1].str());
            const std::string message = dependency_error(source, target);
            if (!message.empty()) {
                const std::string code = message == "cross-application internal dependency" ? "UPD102" : "UPD101";
                add_finding(findings, relative, line_number, code, message, "error", line_text, rules);
            } else {
                const std::string warning = data_commander_warning(source, target);
                if (!warning.empty()) {
                    add_finding(findings, relative, line_number, "UPD103", warning, "warning", line_text, rules);
                }
            }
        }

        if (is_component_role(source.role)) {
            std::smatch method_match;
            int parameter_count = 0;
            bool input_violation = false;
            if (std::regex_search(line_text, method_match, method_pattern)) {
                parameter_count = count_parameters(method_match[1].str());
                input_violation = parameter_count > 1;
            }
            const bool output_violation = std::regex_search(line_text, tuple_return_pattern);

            if (input_violation) {
                if (reducible_lines == 0) {
                    first_offending_line = line_number;
                    first_offending_text = line_text;
                }
                add_finding(findings, relative, line_number, "UPD301", "multiple inputs reduce readability; consider one Input Container", "attention", line_text, rules);
            }
            if (output_violation) {
                if (reducible_lines == 0) {
                    first_offending_line = line_number;
                    first_offending_text = line_text;
                }
                add_finding(findings, relative, line_number, "UPD302", "multiple return values reduce readability; consider one Output Container", "attention", line_text, rules);
            }
            if (input_violation || output_violation) {
                reducible_lines += std::max(0, parameter_count - 1) + (output_violation ? 1 : 0);
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

    const bool substantial_compression =
        reducible_lines >= kMinReducibleLines &&
        static_cast<double>(reducible_lines) /
                static_cast<double>(std::max(1, effective_lines)) >=
            kMinReductionRatio;
    if ((source.role == "commander" || source.role == "messenger") && substantial_compression) {
        add_finding(
            findings,
            relative,
            first_offending_line,
            "UPD303",
            "Compresser/Container introduction is expected to substantially reduce this Commander/Messenger",
            "warning",
            first_offending_text,
            rules);
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

        std::ifstream source_file(path);
        std::ostringstream source_stream;
        source_stream << source_file.rdbuf();
        auto responsibility_findings = check_responsibilities(source_stream.str(), relative, rules);
        findings.insert(findings.end(), responsibility_findings.begin(), responsibility_findings.end());
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
