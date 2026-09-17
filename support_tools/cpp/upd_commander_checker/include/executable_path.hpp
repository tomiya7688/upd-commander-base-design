#pragma once

#include <filesystem>
#include <string>

namespace upd_checker {

std::filesystem::path resolve_executable_path(const std::string& invoked_path);

}  // namespace upd_checker
