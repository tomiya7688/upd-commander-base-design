#pragma once

#include <string>
#include <map>
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
    bool write_baseline = false;
    std::string write_baseline_path;
    std::string baseline_path;
    std::vector<std::string> fail_on;
    bool fail_on_configured = false;
    std::string fail_on_scope = "all";
    std::map<std::string, std::string> severity_overrides;
};

CliOptions parse_cli(int argc, char* argv[], const Config& config);

}  // namespace upd_checker
