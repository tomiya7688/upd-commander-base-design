#pragma once

#include <string>

namespace upd_checker {

struct Finding {
    std::string path;
    int line = 1;
    std::string code;
    std::string message;
    std::string severity = "error";
    std::string symbol;
    std::string context;
};

}  // namespace upd_checker
