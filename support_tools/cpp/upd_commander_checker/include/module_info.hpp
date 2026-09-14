#pragma once

#include <string>

namespace upd_checker {

struct ModuleInfo {
    std::string path;
    std::string layer;
    std::string role;
    std::string application_id;
};

}  // namespace upd_checker
