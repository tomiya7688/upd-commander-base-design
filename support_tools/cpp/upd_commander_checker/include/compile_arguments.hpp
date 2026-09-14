#pragma once

#include <filesystem>
#include <string>
#include <vector>

namespace upd_checker {

std::vector<std::string> resolve_compile_arguments(
    const std::filesystem::path& source_path,
    const std::filesystem::path& scan_root);

}  // namespace upd_checker
