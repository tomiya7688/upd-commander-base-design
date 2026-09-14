#include "compile_arguments.hpp"

#include <clang-c/CXCompilationDatabase.h>

#include <filesystem>
#include <optional>
#include <string>
#include <vector>

namespace upd_checker {
namespace {

std::string cx_text(CXString value) {
    const char* raw = clang_getCString(value);
    std::string result = raw == nullptr ? std::string{} : std::string(raw);
    clang_disposeString(value);
    return result;
}

std::optional<std::filesystem::path> find_database_directory(
    const std::filesystem::path& source_path) {
    std::filesystem::path current = source_path.parent_path();
    while (!current.empty()) {
        if (std::filesystem::exists(current / "compile_commands.json")) {
            return current;
        }
        const auto parent = current.parent_path();
        if (parent == current) {
            break;
        }
        current = parent;
    }
    return std::nullopt;
}

bool path_matches(
    const std::string& argument,
    const std::filesystem::path& command_directory,
    const std::filesystem::path& command_source) {
    if (argument.empty() || argument.front() == '-') {
        return false;
    }
    std::filesystem::path candidate(argument);
    if (candidate.is_relative()) {
        candidate = command_directory / candidate;
    }
    return candidate.lexically_normal() == command_source.lexically_normal();
}

std::vector<std::string> arguments_from_command(CXCompileCommand command) {
    const std::filesystem::path directory(cx_text(clang_CompileCommand_getDirectory(command)));
    std::filesystem::path command_source(cx_text(clang_CompileCommand_getFilename(command)));
    if (command_source.is_relative()) {
        command_source = directory / command_source;
    }

    std::vector<std::string> arguments;
    arguments.push_back("-working-directory=" + directory.string());
    bool skip_next = false;
    const unsigned count = clang_CompileCommand_getNumArgs(command);
    for (unsigned index = 1; index < count; ++index) {
        const std::string argument = cx_text(clang_CompileCommand_getArg(command, index));
        if (skip_next) {
            skip_next = false;
            continue;
        }
        if (argument == "-c" || argument == "-MD" || argument == "-MMD") {
            continue;
        }
        if (argument == "-o" || argument == "-MF" || argument == "-MT" || argument == "-MQ") {
            skip_next = true;
            continue;
        }
        if (argument.rfind("-o", 0) == 0 && argument.size() > 2) {
            continue;
        }
        if (path_matches(argument, directory, command_source)) {
            continue;
        }
        arguments.push_back(argument);
    }
    return arguments;
}

std::vector<std::string> database_arguments(
    const std::filesystem::path& source_path,
    const std::filesystem::path& database_directory) {
    CXCompilationDatabase_Error error = CXCompilationDatabase_NoError;
    CXCompilationDatabase database = clang_CompilationDatabase_fromDirectory(
        database_directory.string().c_str(), &error);
    if (database == nullptr || error != CXCompilationDatabase_NoError) {
        if (database != nullptr) {
            clang_CompilationDatabase_dispose(database);
        }
        return {};
    }

    const std::string source = std::filesystem::absolute(source_path).lexically_normal().string();
    CXCompileCommands commands = clang_CompilationDatabase_getCompileCommands(database, source.c_str());
    if (clang_CompileCommands_getSize(commands) == 0) {
        clang_CompileCommands_dispose(commands);
        commands = clang_CompilationDatabase_getAllCompileCommands(database);
    }

    std::vector<std::string> arguments;
    if (clang_CompileCommands_getSize(commands) > 0) {
        arguments = arguments_from_command(clang_CompileCommands_getCommand(commands, 0));
    }
    clang_CompileCommands_dispose(commands);
    clang_CompilationDatabase_dispose(database);
    return arguments;
}

std::vector<std::string> fallback_arguments(
    const std::filesystem::path& source_path,
    const std::filesystem::path& scan_root) {
    return {
        "-x",
        "c++",
        "-std=c++17",
        "-I" + scan_root.string(),
        "-I" + source_path.parent_path().string(),
    };
}

}  // namespace

std::vector<std::string> resolve_compile_arguments(
    const std::filesystem::path& source_path,
    const std::filesystem::path& scan_root) {
    const auto database_directory = find_database_directory(source_path);
    if (database_directory.has_value()) {
        auto arguments = database_arguments(source_path, *database_directory);
        if (!arguments.empty()) {
            return arguments;
        }
    }
    return fallback_arguments(source_path, scan_root);
}

}  // namespace upd_checker
