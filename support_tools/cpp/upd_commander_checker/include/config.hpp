#pragma once

#include <string>
#include <vector>

#include "config_error.hpp"

namespace upd_checker {

struct Config {
    std::string input = ".";
    std::string output;
    std::vector<std::string> ignore;
    bool warnings_as_errors = false;
    std::vector<std::string> common_roots = {"common", "shared"};
    int upd301_max_inputs = 2;
    int flat_layer_min_files = 12;
    int flat_layer_min_direct_percent = 80;
    int model_group_min_items = 3;
    int model_group_min_occurrences = 2;
    std::vector<std::string> enabled_rules;
    bool enabled_rules_configured = false;
};

Config load_config(const std::string& executable_path);

}  // namespace upd_checker
