#pragma once

#include <string>

namespace upd_checker {

struct BaselineEntry {
    std::string fingerprint;
    std::string rule;
    std::string path;
    std::string symbol;
    std::string context;
    std::string severity;
    int line = 0;
    std::string message;
};

}  // namespace upd_checker
