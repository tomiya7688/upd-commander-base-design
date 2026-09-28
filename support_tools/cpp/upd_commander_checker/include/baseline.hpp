#pragma once

#include <string>

namespace upd_checker {

std::string finding_fingerprint(
    const std::string& rule,
    const std::string& path,
    const std::string& symbol,
    const std::string& context);

}  // namespace upd_checker
