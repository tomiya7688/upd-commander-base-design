#include <cassert>
#include <string>
#include <vector>

#include "cli_options.hpp"

namespace {

upd_checker::CliOptions parse(std::vector<std::string> arguments) {
    std::vector<char*> argv;
    argv.reserve(arguments.size());
    for (auto& argument : arguments) {
        argv.push_back(argument.data());
    }
    return upd_checker::parse_cli(
        static_cast<int>(argv.size()),
        argv.data(),
        upd_checker::Config{});
}

bool fails_with(const std::vector<std::string>& arguments, const std::string& text) {
    try {
        parse(arguments);
    } catch (const upd_checker::CliUsageError& error) {
        return std::string(error.what()).find(text) != std::string::npos;
    }
    return false;
}

void test_unknown_option_fails() {
    assert(fails_with({"checker", "--warning-as-errors", "."}, "unknown option"));
}

void test_missing_values_fail() {
    assert(fails_with({"checker", "--ignore"}, "missing value"));
    assert(fails_with({"checker", "--output"}, "missing value"));
    assert(fails_with({"checker", "--output", "--warnings-as-errors"}, "missing value"));
}

void test_multiple_targets_fail() {
    assert(fails_with({"checker", "first", "second"}, "multiple targets"));
}

void test_valid_arguments_override_config() {
    upd_checker::Config config;
    config.input = "configured";
    config.output = "configured.txt";
    config.ignore = {"old/**"};

    std::vector<std::string> arguments = {
        "checker",
        "--ignore",
        "generated/**",
        "--output",
        "report.txt",
        "--warnings-as-errors",
        "source",
    };
    std::vector<char*> argv;
    for (auto& argument : arguments) {
        argv.push_back(argument.data());
    }
    const auto options = upd_checker::parse_cli(
        static_cast<int>(argv.size()), argv.data(), config);

    assert(options.target == "source");
    assert(options.output == "report.txt");
    assert(options.ignores.size() == 2);
    assert(options.ignores[1] == "generated/**");
    assert(options.warnings_as_errors);
}

}  // namespace

int main() {
    test_unknown_option_fails();
    test_missing_values_fail();
    test_multiple_targets_fail();
    test_valid_arguments_override_config();
    return 0;
}
