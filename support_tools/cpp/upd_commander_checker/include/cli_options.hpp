#pragma once

#include <stdexcept>
#include <string>
#include <vector>

#include "config.hpp"

namespace upd_checker {

struct CliOptions {
    std::string target;
    std::string output;
    std::vector<std::string> ignores;
    bool warnings_as_errors = false;
    bool attentions_as_errors = false;
};

class CliUsageError : public std::runtime_error {
public:
    using std::runtime_error::runtime_error;
};

CliOptions parse_cli(int argc, char* argv[], const Config& config);

}  // namespace upd_checker
