#pragma once

#include <stdexcept>

namespace upd_checker {

class CliUsageError : public std::runtime_error {
public:
    using std::runtime_error::runtime_error;
};

}  // namespace upd_checker
