#pragma once

#include <string>
#include <vector>

namespace upd_checker {

int finish_report(
    const std::vector<std::string>& lines,
    const std::string& output,
    int exit_code);

}  // namespace upd_checker
