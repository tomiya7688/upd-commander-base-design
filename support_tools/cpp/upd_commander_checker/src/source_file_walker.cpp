#include "source_file_walker.hpp"

#include <filesystem>
#include <string>
#include <vector>

namespace upd_checker {
namespace {

bool is_source_file(const std::filesystem::path& path) {
    const auto ext = path.extension().string();
    return ext == ".cpp" || ext == ".cc" || ext == ".cxx" ||
           ext == ".hpp" || ext == ".h" || ext == ".hh" || ext == ".hxx";
}

std::string relative_text(
    const std::filesystem::path& path,
    const std::filesystem::path& root) {
    std::error_code error;
    const auto relative = std::filesystem::relative(path, root, error);
    return error ? path.generic_string() : relative.generic_string();
}

void add_read_failure(
    const std::filesystem::path& path,
    const std::filesystem::path& root,
    const std::error_code& error,
    std::vector<Finding>& findings) {
    findings.push_back(Finding{
        relative_text(path, root),
        1,
        "UPD001",
        "read failed: " + error.message(),
        "error",
    });
}

void walk_directory(
    const std::filesystem::path& directory,
    const std::filesystem::path& root,
    std::vector<std::filesystem::path>& paths,
    std::vector<Finding>& findings) {
    std::error_code open_error;
    std::filesystem::directory_iterator iterator(directory, open_error);
    if (open_error) {
        add_read_failure(directory, root, open_error, findings);
        return;
    }

    const std::filesystem::directory_iterator end;
    while (iterator != end) {
        const auto entry = *iterator;
        std::error_code status_error;
        const auto status = entry.symlink_status(status_error);
        if (status_error) {
            add_read_failure(entry.path(), root, status_error, findings);
        } else if (std::filesystem::is_directory(status) && !std::filesystem::is_symlink(status)) {
            walk_directory(entry.path(), root, paths, findings);
        } else if (std::filesystem::is_regular_file(status) && is_source_file(entry.path())) {
            paths.push_back(entry.path());
        }

        std::error_code increment_error;
        iterator.increment(increment_error);
        if (increment_error) {
            add_read_failure(directory, root, increment_error, findings);
            break;
        }
    }
}

}  // namespace

std::vector<std::filesystem::path> collect_source_files(
    const std::filesystem::path& target,
    const std::filesystem::path& root,
    std::vector<Finding>& findings) {
    std::vector<std::filesystem::path> paths;
    std::error_code status_error;
    const auto status = std::filesystem::symlink_status(target, status_error);
    if (status_error) {
        add_read_failure(target, root, status_error, findings);
        return paths;
    }

    if (std::filesystem::is_regular_file(status)) {
        if (is_source_file(target)) {
            paths.push_back(target);
        }
        return paths;
    }
    if (std::filesystem::is_directory(status)) {
        walk_directory(target, root, paths, findings);
    }
    return paths;
}

}  // namespace upd_checker
