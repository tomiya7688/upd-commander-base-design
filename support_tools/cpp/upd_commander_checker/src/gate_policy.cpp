#include "gate_policy.hpp"

#include <algorithm>
#include <cctype>
#include <set>

namespace upd_checker {
namespace {

bool is_supported_severity(const std::string& value) {
    return value == "error" || value == "warning" || value == "attention";
}

std::string lower_ascii(std::string value) {
    std::transform(value.begin(), value.end(), value.begin(), [](unsigned char ch) {
        return static_cast<char>(std::tolower(ch));
    });
    return value;
}

std::string upper_ascii(std::string value) {
    std::transform(value.begin(), value.end(), value.begin(), [](unsigned char ch) {
        return static_cast<char>(std::toupper(ch));
    });
    return value;
}

bool is_upd_rule(const std::string& value) {
    if (value.size() < 6 || value.compare(0, 3, "UPD") != 0) {
        return false;
    }
    return std::all_of(value.begin() + 3, value.end(), [](unsigned char ch) {
        return ch >= '0' && ch <= '9';
    });
}

std::string trim_ascii(std::string value) {
    const auto is_space = [](unsigned char ch) { return std::isspace(ch) != 0; };
    value.erase(value.begin(), std::find_if_not(value.begin(), value.end(), is_space));
    value.erase(std::find_if_not(value.rbegin(), value.rend(), is_space).base(), value.end());
    return value;
}

bool is_exact_relative_path(const std::string& value) {
    if (value.empty() || value.front() == '/' ||
        value.find_first_of("\\:*?[]") != std::string::npos) {
        return false;
    }
    std::size_t start = 0;
    while (start <= value.size()) {
        const std::size_t separator = value.find('/', start);
        const std::string segment = value.substr(
            start,
            separator == std::string::npos ? std::string::npos : separator - start);
        if (segment.empty() || segment == "." || segment == "..") {
            return false;
        }
        if (separator == std::string::npos) {
            break;
        }
        start = separator + 1;
    }
    return true;
}

}  // namespace

bool normalize_fail_on(
    const std::vector<std::string>& values,
    std::vector<std::string>* normalized) {
    normalized->clear();
    std::set<std::string> seen;
    for (const auto& value : values) {
        const std::string severity = lower_ascii(trim_ascii(value));
        if (!is_supported_severity(severity)) {
            return false;
        }
        if (seen.insert(severity).second) {
            normalized->push_back(severity);
        }
    }
    return true;
}

bool parse_fail_on_argument(
    const std::string& value,
    std::vector<std::string>* normalized) {
    if (value.empty()) {
        normalized->clear();
        return true;
    }
    std::vector<std::string> values;
    std::size_t start = 0;
    while (true) {
        const std::size_t separator = value.find(',', start);
        values.push_back(value.substr(
            start,
            separator == std::string::npos ? std::string::npos : separator - start));
        if (separator == std::string::npos) {
            break;
        }
        start = separator + 1;
    }
    return normalize_fail_on(values, normalized);
}

bool normalize_severity_overrides(
    const std::map<std::string, std::string>& values,
    std::map<std::string, std::string>* normalized) {
    normalized->clear();
    for (const auto& item : values) {
        const std::string rule = upper_ascii(item.first);
        const std::string severity = lower_ascii(item.second);
        if (!is_upd_rule(rule) || !is_supported_severity(severity)) {
            return false;
        }
        (*normalized)[rule] = severity;
    }
    return true;
}

bool normalize_gate_exceptions(
    const std::vector<GateException>& values,
    std::vector<GateException>* normalized) {
    normalized->clear();
    std::set<std::string> seen;
    for (auto value : values) {
        value.rule = upper_ascii(value.rule);
        value.reason = trim_ascii(value.reason);
        if (!is_upd_rule(value.rule) || !is_exact_relative_path(value.path) ||
            value.reason.empty() || value.reason.find_first_of("\r\n") != std::string::npos ||
            value.line < 0) {
            return false;
        }
        std::string key = value.rule;
        key.push_back('\0');
        key += value.path;
        key.push_back('\0');
        key += std::to_string(value.line);
        if (!seen.insert(key).second) {
            return false;
        }
        normalized->push_back(std::move(value));
    }
    return true;
}

std::string gate_exception_reason(
    const Finding& finding,
    const std::vector<GateException>& exceptions) {
    for (const auto& exception : exceptions) {
        if (exception.rule == upper_ascii(finding.code) &&
            exception.path == finding.path &&
            exception.line != 0 && exception.line == finding.line) {
            return exception.reason;
        }
    }
    for (const auto& exception : exceptions) {
        if (exception.rule == upper_ascii(finding.code) &&
            exception.path == finding.path && exception.line == 0) {
            return exception.reason;
        }
    }
    return {};
}

bool parse_severity_override_argument(
    const std::string& value,
    std::string* rule,
    std::string* severity) {
    const std::size_t separator = value.find('=');
    if (separator == std::string::npos) {
        return false;
    }
    std::map<std::string, std::string> normalized;
    if (!normalize_severity_overrides(
            {{value.substr(0, separator), value.substr(separator + 1)}},
            &normalized)) {
        return false;
    }
    *rule = normalized.begin()->first;
    *severity = normalized.begin()->second;
    return true;
}

std::vector<Finding> apply_severity_overrides(
    const std::vector<Finding>& findings,
    const std::map<std::string, std::string>& overrides) {
    std::vector<Finding> result = findings;
    for (auto& finding : result) {
        const auto override = overrides.find(upper_ascii(finding.code));
        if (override != overrides.end()) {
            finding.severity = override->second;
        }
    }
    return result;
}

bool should_fail(
    const std::vector<Finding>& findings,
    const std::vector<std::string>& fail_on) {
    for (const auto& finding : findings) {
        if (std::find(fail_on.begin(), fail_on.end(), finding.severity) != fail_on.end()) {
            return true;
        }
    }
    return false;
}

}  // namespace upd_checker
