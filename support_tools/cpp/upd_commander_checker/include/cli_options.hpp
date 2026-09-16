#pragma once

#include <string>
#include <vector>

#include "cli_usage_error.hpp"
#include "config.hpp"

namespace upd_checker {

struct CliOptions {
    std::string target;
    std::string output;
    std::vector<std::string> ignores;
    bool warnings_as_errors = false;
    bool attentions_as_errors = false;
};

CliOptions parse_cli(int argc, char* argv[], const Config& config);

}  // namespace upd_checker
