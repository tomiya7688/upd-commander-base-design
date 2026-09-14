#pragma once

#include <stdexcept>
#include <string>
#include <vector>

namespace upd_checker {

struct Config {
    std::string input = ".";
    std::string output;
    std::vector<std::string> ignore;
    bool warnings_as_errors = false;
};

class ConfigError : public std::runtime_error {
public:
    explicit ConfigError(const std::string& message) : std::runtime_error(message) {}
};

Config load_config(const std::string& executable_path);

}  // namespace upd_checker
