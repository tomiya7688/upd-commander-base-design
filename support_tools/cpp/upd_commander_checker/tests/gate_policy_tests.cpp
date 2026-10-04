#include <cassert>
#include <map>
#include <string>
#include <vector>

#include "gate_policy.hpp"

int main() {
    std::vector<std::string> fail_on;
    assert(upd_checker::normalize_fail_on({" ERROR ", "warning", "error"}, &fail_on));
    assert((fail_on == std::vector<std::string>{"error", "warning"}));
    assert(upd_checker::parse_fail_on_argument("", &fail_on));
    assert(fail_on.empty());
    assert(!upd_checker::parse_fail_on_argument("error,", &fail_on));
    assert(!upd_checker::normalize_fail_on({"fatal"}, &fail_on));

    std::map<std::string, std::string> overrides;
    assert(upd_checker::normalize_severity_overrides(
        {{"upd101", "WARNING"}, {"UPD9999", "attention"}}, &overrides));
    assert(overrides.at("UPD101") == "warning");
    assert(overrides.at("UPD9999") == "attention");
    assert(!upd_checker::normalize_severity_overrides({{"UPD101", "fatal"}}, &overrides));
    assert(!upd_checker::normalize_severity_overrides({{" UPD101", "error"}}, &overrides));

    std::string rule;
    std::string severity;
    assert(upd_checker::parse_severity_override_argument(
        "upd203=warning", &rule, &severity));
    assert(rule == "UPD203" && severity == "warning");
    assert(!upd_checker::parse_severity_override_argument(
        "UPD203 warning", &rule, &severity));

    const std::vector<upd_checker::Finding> findings = {
        {"src/a.cpp", 2, "UPD203", "direct I/O", "error"},
        {"src/b.cpp", 3, "UPD301", "container candidate", "attention"},
    };
    const auto effective = upd_checker::apply_severity_overrides(
        findings, {{"UPD203", "warning"}});
    assert(effective.size() == findings.size());
    assert(findings.front().severity == "error");
    assert(effective.front().severity == "warning");
    assert(!upd_checker::should_fail(effective, {"error"}));
    assert(upd_checker::should_fail(effective, {"warning"}));
    assert(!upd_checker::should_fail(effective, {}));
    return 0;
}
