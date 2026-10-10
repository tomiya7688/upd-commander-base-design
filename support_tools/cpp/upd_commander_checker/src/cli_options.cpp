#include "cli_options.hpp"
#include "gate_policy.hpp"

#include <string>

namespace upd_checker {
namespace {

bool starts_with_dash(const std::string& value) {
    return !value.empty() && value.front() == '-';
}

std::string read_value(int& index, int argc, char* argv[], const std::string& option) {
    if (index + 1 >= argc || starts_with_dash(argv[index + 1])) {
        throw CliUsageError("missing value for " + option);
    }
    return argv[++index];
}

}  // namespace

CliOptions parse_cli(int argc, char* argv[], const Config& config) {
    CliOptions options{
        config.input,
        config.output,
        config.ignore,
        config.warnings_as_errors,
        false,
    };
    options.fail_on = config.fail_on;
    options.fail_on_configured = config.fail_on_configured;
    options.severity_overrides = config.severity_overrides;
    bool target_specified = false;

    for (int index = 1; index < argc; ++index) {
        const std::string argument = argv[index];
        if (argument == "--ignore") {
            options.ignores.push_back(read_value(index, argc, argv, argument));
        } else if (argument == "--output") {
            options.output = read_value(index, argc, argv, argument);
        } else if (argument == "--warnings-as-errors") {
            options.warnings_as_errors = true;
        } else if (argument == "--attentions-as-errors") {
            options.attentions_as_errors = true;
        } else if (argument == "--fail-on") {
            const std::string value = read_value(index, argc, argv, argument);
            if (!parse_fail_on_argument(value, &options.fail_on)) {
                throw CliUsageError("invalid value for --fail-on");
            }
            options.fail_on_configured = true;
        } else if (argument == "--severity-override") {
            const std::string value = read_value(index, argc, argv, argument);
            std::string rule;
            std::string severity;
            if (!parse_severity_override_argument(value, &rule, &severity)) {
                throw CliUsageError(
                    "invalid value for --severity-override; expected UPDnnn=severity");
            }
            options.severity_overrides[rule] = severity;
        } else if (starts_with_dash(argument)) {
            throw CliUsageError("unknown option: " + argument);
        } else if (target_specified) {
            throw CliUsageError("multiple targets: " + argument);
        } else {
            options.target = argument;
            target_specified = true;
        }
    }

    return options;
}

}  // namespace upd_checker
