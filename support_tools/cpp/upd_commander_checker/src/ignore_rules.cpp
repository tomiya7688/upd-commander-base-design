#include "ignore_rules.hpp"

#include <filesystem>
#include <fstream>
#include <regex>
#include <sstream>

namespace upd_checker {
namespace {

std::string trim(const std::string& value) {
    const auto begin = value.find_first_not_of(" \t\r\n");
    if (begin == std::string::npos) {
        return {};
    }
    const auto end = value.find_last_not_of(" \t\r\n");
    return value.substr(begin, end - begin + 1);
}

std::string regex_escape(char ch) {
    const std::string special = R"(.^$|()[]{}+?\)";
    if (special.find(ch) != std::string::npos) {
        return std::string("\\") + ch;
    }
    return std::string(1, ch);
}

std::string glob_to_regex(const std::string& pattern) {
    std::string output = "^";
    for (std::size_t index = 0; index < pattern.size(); ++index) {
        const char ch = pattern[index];
        if (ch == '*') {
            if (index + 1 < pattern.size() && pattern[index + 1] == '*') {
                output += ".*";
                ++index;
            } else {
                output += "[^/]*";
            }
        } else if (ch == '?') {
            output += "[^/]";
        } else {
            output += regex_escape(ch);
        }
    }
    output += "$";
    return output;
}

}  // namespace

std::vector<IgnoreRule> load_ignore_rules(const std::string& root) {
    std::ifstream file(std::filesystem::path(root) / ".updcommanderignore");
    if (!file) {
        return {};
    }

    std::vector<IgnoreRule> rules;
    std::string line;
    while (std::getline(file, line)) {
        line = trim(line);
        if (line.empty() || line.front() == '#') {
            continue;
        }
        const auto comment = line.find('#');
        const std::string main_part = trim(line.substr(0, comment));
        std::istringstream stream(main_part);
        std::string first;
        std::string second;
        stream >> first >> second;
        if (first.empty()) {
            continue;
        }
        if (second.empty()) {
            rules.push_back(IgnoreRule{"all", first});
        } else {
            rules.push_back(IgnoreRule{first, second});
        }
    }
    return rules;
}

bool glob_match(const std::string& path, const std::string& pattern) {
    try {
        return std::regex_match(path, std::regex(glob_to_regex(pattern)));
    } catch (const std::regex_error&) {
        return false;
    }
}

bool is_ignored(
    const std::string& path,
    const std::string& code,
    const std::string& line_text,
    const std::vector<IgnoreRule>& rules) {
    for (const auto& rule : rules) {
        if (glob_match(path, rule.pattern) && (rule.code == "all" || rule.code == code)) {
            return true;
        }
    }
    return line_text.find("upd: ignore " + code) != std::string::npos ||
           line_text.find("upd: ignore all") != std::string::npos;
}

}  // namespace upd_checker
