#include "model_attention_rules.hpp"

#include <clang-c/Index.h>

#include <algorithm>
#include <cctype>
#include <fstream>
#include <regex>
#include <sstream>
#include <string>
#include <unordered_map>
#include <unordered_set>
#include <vector>

#include "classifier.hpp"
#include "compile_arguments.hpp"

namespace upd_checker {
namespace {

struct CollectionState {
    ModuleInfo module;
    std::string relative;
    std::vector<std::string> lines;
    const std::vector<IgnoreRule>* rules = nullptr;
    int min_items = 3;
    std::vector<ModelGroupOccurrence> occurrences;
    std::unordered_map<std::string, std::vector<std::string>> parallel_groups;
    std::unordered_map<std::string, int> parallel_lines;
};

std::string cx_text(CXString value) {
    const char* raw = clang_getCString(value);
    std::string result = raw == nullptr ? std::string{} : std::string(raw);
    clang_disposeString(value);
    return result;
}

std::string normalize_item(std::string value) {
    while (!value.empty() && value.front() == '_') {
        value.erase(value.begin());
    }
    std::transform(value.begin(), value.end(), value.begin(), [](unsigned char ch) {
        return static_cast<char>(std::tolower(ch));
    });
    return value;
}

int cursor_line(CXCursor cursor) {
    unsigned line = 1;
    clang_getSpellingLocation(clang_getCursorLocation(cursor), nullptr, &line, nullptr, nullptr);
    return static_cast<int>(line);
}

std::string line_text(const CollectionState& state, int line) {
    if (line <= 0 || line > static_cast<int>(state.lines.size())) {
        return {};
    }
    return state.lines[static_cast<std::size_t>(line - 1)];
}

bool all_unique(const std::vector<std::string>& items) {
    std::unordered_set<std::string> seen;
    for (const auto& item : items) {
        if (item.empty() || !seen.insert(item).second) {
            return false;
        }
    }
    return true;
}

void add_occurrence(
    CollectionState& state,
    int line,
    const std::string& kind,
    const std::vector<std::string>& items) {
    if (static_cast<int>(items.size()) < state.min_items || !all_unique(items)) {
        return;
    }
    if (is_ignored(state.relative, "UPD406", line_text(state, line), *state.rules)) {
        return;
    }
    state.occurrences.push_back(ModelGroupOccurrence{
        state.relative,
        line,
        state.module.application_id,
        state.module.layer,
        kind,
        items,
    });
}

std::string terminal_reference_key(CXCursor cursor) {
    const auto kind = clang_getCursorKind(cursor);
    if (kind == CXCursor_DeclRefExpr || kind == CXCursor_MemberRefExpr ||
        kind == CXCursor_MemberRef) {
        return normalize_item(cx_text(clang_getCursorSpelling(cursor)));
    }

    std::string result;
    clang_visitChildren(
        cursor,
        [](CXCursor child, CXCursor, CXClientData client_data) {
            auto* output = static_cast<std::string*>(client_data);
            const auto kind = clang_getCursorKind(child);
            if (kind == CXCursor_DeclRefExpr || kind == CXCursor_MemberRefExpr ||
                kind == CXCursor_MemberRef) {
                const std::string value =
                    normalize_item(cx_text(clang_getCursorSpelling(child)));
                if (!value.empty()) {
                    *output = value;
                }
            }
            return CXChildVisit_Recurse;
        },
        &result);
    return result;
}

std::vector<std::string> function_parameter_keys(CXCursor cursor) {
    std::vector<std::string> items;
    const int count = clang_Cursor_getNumArguments(cursor);
    if (count < 0) {
        return items;
    }
    for (int index = 0; index < count; ++index) {
        const std::string key =
            normalize_item(cx_text(clang_getCursorSpelling(
                clang_Cursor_getArgument(cursor, index))));
        if (key.empty()) {
            return {};
        }
        items.push_back(key);
    }
    if (clang_Cursor_isVariadic(cursor) != 0) {
        items.push_back("variadic");
    }
    return items;
}

std::vector<std::string> call_argument_keys(CXCursor cursor) {
    std::vector<std::string> items;
    const int count = clang_Cursor_getNumArguments(cursor);
    if (count < 0) {
        return items;
    }
    for (int index = 0; index < count; ++index) {
        const std::string key = terminal_reference_key(
            clang_Cursor_getArgument(cursor, index));
        if (key.empty()) {
            return {};
        }
        items.push_back(key);
    }
    return items;
}

std::string cursor_tokens(CXCursor cursor) {
    CXTranslationUnit unit = clang_Cursor_getTranslationUnit(cursor);
    CXToken* tokens = nullptr;
    unsigned count = 0;
    clang_tokenize(unit, clang_getCursorExtent(cursor), &tokens, &count);
    std::string result;
    for (unsigned index = 0; index < count; ++index) {
        result += cx_text(clang_getTokenSpelling(unit, tokens[index]));
    }
    if (tokens != nullptr) {
        clang_disposeTokens(unit, tokens, count);
    }
    return result;
}

void collect_parallel_access(CollectionState& state, CXCursor cursor) {
    static const std::regex indexed(
        R"(^([A-Za-z_][A-Za-z0-9_]*)\[([A-Za-z_][A-Za-z0-9_]*|[0-9]+)\]$)");
    const std::string tokens = cursor_tokens(cursor);
    std::smatch match;
    if (!std::regex_match(tokens, match, indexed)) {
        return;
    }
    const std::string collection = normalize_item(match[1].str());
    const std::string index = normalize_item(match[2].str());
    const int line = cursor_line(cursor);
    const std::string key = std::to_string(line) + "\x1f" + index;
    auto& items = state.parallel_groups[key];
    if (std::find(items.begin(), items.end(), collection) == items.end()) {
        items.push_back(collection);
    }
    state.parallel_lines[key] = line;
}

CXChildVisitResult visit_cursor(CXCursor cursor, CXCursor, CXClientData client_data) {
    auto& state = *static_cast<CollectionState*>(client_data);
    if (clang_Location_isFromMainFile(clang_getCursorLocation(cursor)) == 0) {
        return CXChildVisit_Continue;
    }

    const auto kind = clang_getCursorKind(cursor);
    if ((kind == CXCursor_FunctionDecl || kind == CXCursor_CXXMethod ||
         kind == CXCursor_FunctionTemplate) &&
        clang_isCursorDefinition(cursor) != 0) {
        add_occurrence(
            state,
            cursor_line(cursor),
            "parameters",
            function_parameter_keys(cursor));
    } else if (kind == CXCursor_CallExpr) {
        const std::string call_name =
            normalize_item(cx_text(clang_getCursorSpelling(cursor)));
        if (call_name == "make_tuple" || call_name == "tie") {
            add_occurrence(
                state,
                cursor_line(cursor),
                "tuple",
                call_argument_keys(cursor));
        }
    } else if (kind == CXCursor_ArraySubscriptExpr) {
        collect_parallel_access(state, cursor);
    }
    return CXChildVisit_Recurse;
}

}  // namespace

std::string ModelGroupOccurrence::signature() const {
    std::string result =
        application_id + "\x1f" + layer + "\x1f" + kind + "\x1f";
    for (std::size_t index = 0; index < items.size(); ++index) {
        if (index > 0) {
            result += "\x1e";
        }
        result += items[index];
    }
    return result;
}

std::vector<ModelGroupOccurrence> collect_cpp_model_group_occurrences(
    const std::filesystem::path& path,
    const std::filesystem::path& root,
    const std::string& relative,
    const std::vector<IgnoreRule>& rules,
    int min_items) {
    CollectionState state;
    state.module = classify_path(relative);
    state.relative = relative;
    state.rules = &rules;
    state.min_items = min_items;

    std::ifstream source(path);
    if (!source) {
        return {};
    }
    std::string line;
    while (std::getline(source, line)) {
        state.lines.push_back(line);
    }

    const std::vector<std::string> argument_storage =
        resolve_compile_arguments(path, root);
    std::vector<const char*> arguments;
    arguments.reserve(argument_storage.size());
    for (const auto& argument : argument_storage) {
        arguments.push_back(argument.c_str());
    }

    CXIndex index = clang_createIndex(0, 0);
    const std::string filename = path.string();
    CXTranslationUnit unit = clang_parseTranslationUnit(
        index,
        filename.c_str(),
        arguments.data(),
        static_cast<int>(arguments.size()),
        nullptr,
        0,
        CXTranslationUnit_DetailedPreprocessingRecord | CXTranslationUnit_KeepGoing);
    if (unit == nullptr) {
        clang_disposeIndex(index);
        return {};
    }

    clang_visitChildren(clang_getTranslationUnitCursor(unit), visit_cursor, &state);

    for (const auto& entry : state.parallel_groups) {
        const auto line_iterator = state.parallel_lines.find(entry.first);
        const int occurrence_line =
            line_iterator == state.parallel_lines.end() ? 1 : line_iterator->second;
        add_occurrence(
            state,
            occurrence_line,
            "parallel_collection",
            entry.second);
    }

    clang_disposeTranslationUnit(unit);
    clang_disposeIndex(index);
    return state.occurrences;
}

std::vector<Finding> model_attention_findings(
    const std::vector<ModelGroupOccurrence>& occurrences,
    int min_occurrences) {
    std::unordered_map<std::string, std::vector<ModelGroupOccurrence>> groups;
    for (const auto& occurrence : occurrences) {
        groups[occurrence.signature()].push_back(occurrence);
    }

    std::vector<Finding> findings;
    for (auto& entry : groups) {
        auto& group = entry.second;
        if (static_cast<int>(group.size()) < min_occurrences) {
            continue;
        }
        std::sort(group.begin(), group.end(), [](const auto& left, const auto& right) {
            if (left.path != right.path) {
                return left.path < right.path;
            }
            return left.line < right.line;
        });
        const auto& first = group.front();
        std::ostringstream items;
        for (std::size_t index = 0; index < first.items.size(); ++index) {
            if (index > 0) {
                items << ',';
            }
            items << first.items[index];
        }
        findings.push_back(Finding{
            first.path,
            first.line,
            "UPD406",
            "repeated value group may benefit from a Model/DTO; items=" +
                items.str() + " occurrences=" + std::to_string(group.size()) +
                " kind=" + first.kind,
            "attention",
        });
    }
    return findings;
}

}  // namespace upd_checker
