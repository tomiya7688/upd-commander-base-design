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
};

Config load_config(const std::string& executable_path);

}  // namespace upd_checker
