#include "data_type_location_rules.hpp"

#include <clang-c/Index.h>

#include <filesystem>
#include <fstream>
#include <string>
#include <vector>

namespace upd_checker {
namespace {

struct DataTypeCandidate {
    std::filesystem::path path;
    std::string relative;
    std::string name;
    int line = 1;
};

std::string cx_text(CXString value) {
    const char* raw = clang_getCString(value);
    std::string result = raw == nullptr ? std::string{} : std::string(raw);
    clang_disposeString(value);
    return result;
}

std::string relative_text(
    const std::filesystem::path& path,
    const std::filesystem::path& root) {
    std::error_code error;
    const auto relative = std::filesystem::relative(path, root, error);
    return error ? path.generic_string() : relative.generic_string();
}

std::string canonical_text(const std::filesystem::path& path) {
    std::error_code error;
    const auto value = std::filesystem::weakly_canonical(path, error);
    return (error ? std::filesystem::absolute(path) : value).generic_string();
}

int cursor_line(CXCursor cursor) {
    unsigned line = 1;
    clang_getSpellingLocation(clang_getCursorLocation(cursor), nullptr, &line, nullptr, nullptr);
    return static_cast<int>(line);
}

bool is_file_scope_type(CXCursor cursor) {
    const auto parent_kind = clang_getCursorKind(clang_getCursorSemanticParent(cursor));
    return parent_kind == CXCursor_TranslationUnit || parent_kind == CXCursor_Namespace;
}

bool has_behavior(CXCursor cursor) {
    bool behavior = false;
    clang_visitChildren(
        cursor,
        [](CXCursor child, CXCursor, CXClientData client_data) {
            const auto kind = clang_getCursorKind(child);
            if (kind == CXCursor_CXXMethod || kind == CXCursor_Constructor ||
                kind == CXCursor_Destructor || kind == CXCursor_FunctionTemplate) {
                *static_cast<bool*>(client_data) = true;
                return CXChildVisit_Break;
            }
            return CXChildVisit_Continue;
        },
        &behavior);
    return behavior;
}

std::vector<DataTypeCandidate> data_types_in_file(
    const std::filesystem::path& path,
    const std::filesystem::path& root) {
    std::vector<DataTypeCandidate> result;
    const std::string include_root = "-I" + root.string();
    const std::string include_parent = "-I" + path.parent_path().string();
    const char* arguments[] = {
        "-x", "c++", "-std=c++17", include_root.c_str(), include_parent.c_str()};
    CXIndex index = clang_createIndex(0, 0);
    const std::string filename = path.string();
    CXTranslationUnit unit = clang_parseTranslationUnit(
        index,
        filename.c_str(),
        arguments,
        static_cast<int>(sizeof(arguments) / sizeof(arguments[0])),
        nullptr,
        0,
        CXTranslationUnit_KeepGoing);
    if (unit == nullptr) {
        clang_disposeIndex(index);
        return result;
    }

    struct State {
        std::filesystem::path path;
        std::string relative;
        std::vector<DataTypeCandidate>* result;
    } state{path, relative_text(path, root), &result};

    clang_visitChildren(
        clang_getTranslationUnitCursor(unit),
        [](CXCursor cursor, CXCursor, CXClientData client_data) {
            auto& state = *static_cast<State*>(client_data);
            if (clang_Location_isFromMainFile(clang_getCursorLocation(cursor)) == 0) {
                return CXChildVisit_Continue;
            }
            const auto kind = clang_getCursorKind(cursor);
            if ((kind == CXCursor_ClassDecl || kind == CXCursor_StructDecl ||
                 kind == CXCursor_ClassTemplate) &&
                is_file_scope_type(cursor) && clang_isCursorDefinition(cursor)) {
                const std::string name = cx_text(clang_getCursorSpelling(cursor));
                if (has_behavior(cursor)) {
                    state.result->push_back(
                        DataTypeCandidate{state.path, state.relative, std::string{}, cursor_line(cursor)});
                } else if (!name.empty()) {
                    state.result->push_back(
                        DataTypeCandidate{state.path, state.relative, name, cursor_line(cursor)});
                }
            }
            return CXChildVisit_Recurse;
        },
        &state);

    clang_disposeTranslationUnit(unit);
    clang_disposeIndex(index);
    return result;
}

bool referenced_from_file(
    const DataTypeCandidate& candidate,
    const std::filesystem::path& path,
    const std::filesystem::path& root) {
    const std::string include_root = "-I" + root.string();
    const std::string include_parent = "-I" + path.parent_path().string();
    const char* arguments[] = {
        "-x", "c++", "-std=c++17", include_root.c_str(), include_parent.c_str()};
    CXIndex index = clang_createIndex(0, 0);
    const std::string filename = path.string();
    CXTranslationUnit unit = clang_parseTranslationUnit(
        index,
        filename.c_str(),
        arguments,
        static_cast<int>(sizeof(arguments) / sizeof(arguments[0])),
        nullptr,
        0,
        CXTranslationUnit_KeepGoing);
    if (unit == nullptr) {
        clang_disposeIndex(index);
        return false;
    }

    struct RefState {
        std::string declaration_path;
        bool found = false;
    } state{canonical_text(candidate.path), false};

    clang_visitChildren(
        clang_getTranslationUnitCursor(unit),
        [](CXCursor cursor, CXCursor, CXClientData client_data) {
            auto& state = *static_cast<RefState*>(client_data);
            const auto kind = clang_getCursorKind(cursor);
            if (kind != CXCursor_TypeRef && kind != CXCursor_TemplateRef) {
                return state.found ? CXChildVisit_Break : CXChildVisit_Recurse;
            }
            CXCursor referenced = clang_getCursorReferenced(cursor);
            if (clang_Cursor_isNull(referenced)) {
                return CXChildVisit_Continue;
            }
            CXFile file = nullptr;
            clang_getSpellingLocation(
                clang_getCursorLocation(referenced), &file, nullptr, nullptr, nullptr);
            if (file == nullptr) {
                return CXChildVisit_Continue;
            }
            const std::string declaration = canonical_text(cx_text(clang_getFileName(file)));
            if (declaration == state.declaration_path) {
                state.found = true;
                return CXChildVisit_Break;
            }
            return CXChildVisit_Continue;
        },
        &state);

    clang_disposeTranslationUnit(unit);
    clang_disposeIndex(index);
    return state.found;
}

std::string line_text(const std::filesystem::path& path, int wanted) {
    std::ifstream input(path);
    std::string line;
    for (int current = 1; std::getline(input, line); ++current) {
        if (current == wanted) {
            return line;
        }
    }
    return {};
}

}  // namespace

std::vector<Finding> check_data_type_locations(
    const std::vector<std::filesystem::path>& paths,
    const std::filesystem::path& root,
    const std::vector<IgnoreRule>& rules) {
    std::vector<DataTypeCandidate> candidates;
    for (const auto& path : paths) {
        auto local = data_types_in_file(path, root);
        if (local.size() < 2) {
            continue;
        }
        for (const auto& item : local) {
            if (!item.name.empty()) {
                candidates.push_back(item);
            }
        }
    }

    std::vector<Finding> findings;
    for (const auto& candidate : candidates) {
        bool external = false;
        for (const auto& path : paths) {
            if (canonical_text(path) == canonical_text(candidate.path)) {
                continue;
            }
            if (referenced_from_file(candidate, path, root)) {
                external = true;
                break;
            }
        }
        const std::string code = external ? "UPD404" : "UPD403";
        if (is_ignored(
                candidate.relative,
                code,
                line_text(candidate.path, candidate.line),
                rules)) {
            continue;
        }
        findings.push_back(Finding{
            candidate.relative,
            candidate.line,
            code,
            external
                ? "data-only type " + candidate.name +
                      " shares a file with another type and is referenced from another file"
                : "data-only type " + candidate.name + " shares a file with another type",
            external ? "warning" : "attention"});
    }
    return findings;
}

}  // namespace upd_checker
