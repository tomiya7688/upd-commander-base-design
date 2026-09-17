#include <cassert>
#include <cstdlib>
#include <filesystem>
#include <fstream>
#include <string>

#include "config.hpp"
#include "executable_path.hpp"

namespace {

void write_file(const std::filesystem::path& path, const std::string& content) {
    std::filesystem::create_directories(path.parent_path());
    std::ofstream file(path);
    file << content;
}

std::string command_name() {
#ifdef _WIN32
    return "upd-commander-check.exe";
#else
    return "upd-commander-check";
#endif
}

char path_separator() {
#ifdef _WIN32
    return ';';
#else
    return ':';
#endif
}

void set_environment(const char* name, const std::string& value) {
#ifdef _WIN32
    _putenv_s(name, value.c_str());
#else
    setenv(name, value.c_str(), 1);
#endif
}

void unset_environment(const char* name) {
#ifdef _WIN32
    _putenv_s(name, "");
#else
    unsetenv(name);
#endif
}

void restore_environment(const char* name, bool existed, const std::string& value) {
    if (existed) {
        set_environment(name, value);
    } else {
        unset_environment(name);
    }
}

std::filesystem::path normalized_absolute(const std::filesystem::path& path) {
    return std::filesystem::absolute(path).lexically_normal();
}

void prepare_executable_config(
    const std::filesystem::path& executable,
    const std::string& input) {
    write_file(executable, "test executable\n");
    write_file(
        executable.parent_path() / "config" / "path.json",
        "{\"input\":\"" + input + "\"}\n");
}

void assert_input(const upd_checker::Config& config, const std::filesystem::path& expected) {
    assert(normalized_absolute(config.input) == normalized_absolute(expected));
}

void test_absolute_executable_path() {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_config_absolute_path";
    std::filesystem::remove_all(root);
    const auto executable = root / "bin" / command_name();
    prepare_executable_config(executable, "absolute-input");

    const auto config = upd_checker::load_config(executable.string());
    assert_input(config, executable.parent_path() / "absolute-input");
    std::filesystem::remove_all(root);
}

void test_relative_executable_path() {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_config_relative_path";
    std::filesystem::remove_all(root);
    const auto executable = root / "bin" / command_name();
    prepare_executable_config(executable, "relative-input");

    const auto old_cwd = std::filesystem::current_path();
    std::filesystem::current_path(root);
    const auto config = upd_checker::load_config(
        (std::filesystem::path("bin") / command_name()).string());
    assert_input(config, executable.parent_path() / "relative-input");
    std::filesystem::current_path(old_cwd);
    std::filesystem::remove_all(root);
}

void test_path_lookup_prefers_executable_config() {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_config_path_lookup";
    std::filesystem::remove_all(root);
    const auto executable = root / "bin" / command_name();
    prepare_executable_config(executable, "path-input");
    write_file(root / "cwd" / "config" / "path.json", "{\"input\":\"cwd-input\"}\n");

    const char* current_path_value = std::getenv("PATH");
    const bool path_existed = current_path_value != nullptr;
    const std::string old_path = path_existed ? current_path_value : "";
    const std::string suffix = path_existed ? std::string(1, path_separator()) + old_path : "";
    const auto old_cwd = std::filesystem::current_path();
    set_environment("PATH", executable.parent_path().string() + suffix);
    std::filesystem::current_path(root / "cwd");

    const auto resolved = upd_checker::resolve_executable_path(command_name());
    assert(normalized_absolute(resolved) == normalized_absolute(executable));
    const auto config = upd_checker::load_config(command_name());
    assert_input(config, executable.parent_path() / "path-input");

    std::filesystem::current_path(old_cwd);
    restore_environment("PATH", path_existed, old_path);
    std::filesystem::remove_all(root);
}

void test_current_directory_config_is_fallback() {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_config_cwd_fallback";
    std::filesystem::remove_all(root);
    const auto executable = root / "bin" / command_name();
    write_file(executable, "test executable\n");
    write_file(root / "cwd" / "config" / "path.json", "{\"input\":\"cwd-input\"}\n");

    const char* current_path_value = std::getenv("PATH");
    const bool path_existed = current_path_value != nullptr;
    const std::string old_path = path_existed ? current_path_value : "";
    const auto old_cwd = std::filesystem::current_path();
    set_environment("PATH", executable.parent_path().string());
    std::filesystem::current_path(root / "cwd");

    const auto config = upd_checker::load_config(command_name());
    assert_input(config, root / "cwd" / "cwd-input");

    std::filesystem::current_path(old_cwd);
    restore_environment("PATH", path_existed, old_path);
    std::filesystem::remove_all(root);
}

}  // namespace

int main() {
    test_absolute_executable_path();
    test_relative_executable_path();
    test_path_lookup_prefers_executable_config();
    test_current_directory_config_is_fallback();
    return 0;
}
