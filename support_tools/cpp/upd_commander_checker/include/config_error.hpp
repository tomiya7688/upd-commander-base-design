#pragma once

#include <stdexcept>
#include <string>

namespace upd_checker {

class ConfigError : public std::runtime_error {
public:
    explicit ConfigError(const std::string& message) : std::runtime_error(message) {}
};

}  // namespace upd_checker
