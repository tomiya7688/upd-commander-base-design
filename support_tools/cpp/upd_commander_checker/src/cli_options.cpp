#include "cli_options.hpp"

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
