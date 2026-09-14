#include "ast_analyzer.hpp"

#include <clang-c/Index.h>

#include <algorithm>
#include <cctype>
#include <fstream>
#include <string>
#include <vector>

#include "classifier.hpp"
#include "compile_arguments.hpp"
#include "dependency_rules.hpp"

namespace upd_checker {
namespace {

constexpr int kMinReducibleLines = 10;
constexpr double kMinReductionRatio = 0.20;
constexpr int kMaxResponsibilityLines = 250;
constexpr int kMaxResponsibilityMethods = 12;

struct AnalysisState {
    ModuleInfo source;
    std::string relative;
    std::vector<std::string> lines;
    const std::vector<IgnoreRule>* rules;
    std::vector<Finding> findings;
    int effective_lines = 0;
    int reducible_lines = 0;
    int first_offending_line = 1;
    int major_class_count = 0;
    int second_class_line = 0;
};

std::string cx_text(CXString value) {
    const char* raw = clang_getCString(value);
    std::string result = raw == nullptr ? std::string{} : std::string(raw);
    clang_disposeString(value);
    return result;
}

std::string lower(std::string value) {
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

int cursor_end_line(CXCursor cursor) {
    unsigned line = 1;
    clang_getSpellingLocation(
        clang_getRangeEnd(clang_getCursorExtent(cursor)), nullptr, &line, nullptr, nullptr);
    return static_cast<int>(line);
}

bool is_main_file_cursor(CXCursor cursor) {
    return clang_Location_isFromMainFile(clang_getCursorLocation(cursor)) != 0;
}

std::string line_text(const AnalysisState& state, int line) {
    if (line <= 0 || line > static_cast<int>(state.lines.size())) {
        return {};
    }
    return state.lines[static_cast<std::size_t>(line - 1)];
}

void add_finding(
    AnalysisState& state,
    int line,
    const std::string& code,
    const std::string& message,
    const std::string& severity) {
    const std::string text = line_text(state, line);
    if (!is_ignored(state.relative, code, text, *state.rules)) {
        state.findings.push_back(Finding{state.relative, line, code, message, severity});
    }
}

bool is_component_role(const std::string& role) {
    return role == "commander" || role == "messenger" || role == "processing";
}

bool is_arithmetic(CXCursor cursor) {
    unsigned cursor_offset = 0;
    clang_getSpellingLocation(
        clang_getCursorLocation(cursor), nullptr, nullptr, nullptr, &cursor_offset);

    CXToken* tokens = nullptr;
    unsigned token_count = 0;
    CXTranslationUnit unit = clang_Cursor_getTranslationUnit(cursor);
    clang_tokenize(unit, clang_getCursorExtent(cursor), &tokens, &token_count);
    bool arithmetic = false;
    for (unsigned index = 0; index < token_count; ++index) {
        unsigned token_offset = 0;
        clang_getSpellingLocation(
            clang_getTokenLocation(unit, tokens[index]), nullptr, nullptr, nullptr, &token_offset);
        if (token_offset != cursor_offset) {
            continue;
        }
        const std::string token = cx_text(clang_getTokenSpelling(unit, tokens[index]));
        arithmetic = token == "+" || token == "-" || token == "*" ||
                     token == "/" || token == "%";
        break;
    }
    if (tokens != nullptr) {
        clang_disposeTokens(unit, tokens, token_count);
    }
    return arithmetic;
}

std::string qualified_name(CXCursor cursor) {
    std::vector<std::string> names;
    CXCursor current = cursor;
    while (!clang_Cursor_isNull(current)) {
        if (clang_getCursorKind(current) == CXCursor_TranslationUnit) {
            break;
        }
        const std::string name = cx_text(clang_getCursorSpelling(current));
        if (!name.empty()) {
            names.push_back(name);
        }
        current = clang_getCursorSemanticParent(current);
    }
    std::reverse(names.begin(), names.end());
    std::string result;
    for (const auto& name : names) {
        if (!result.empty()) {
            result += "::";
        }
        result += name;
    }
    return result;
}

bool is_direct_work(CXCursor cursor) {
    const auto kind = clang_getCursorKind(cursor);
    if (kind == CXCursor_VarDecl) {
        const CXType canonical = clang_getCanonicalType(clang_getCursorType(cursor));
        const std::string type = lower(cx_text(clang_getTypeSpelling(canonical)));
        return type.find("std::basic_ifstream") != std::string::npos ||
               type.find("std::basic_ofstream") != std::string::npos ||
               type.find("std::basic_fstream") != std::string::npos;
    }
    if (kind != CXCursor_CallExpr) {
        return false;
    }

    CXCursor referenced = clang_getCursorReferenced(cursor);
    if (!clang_Cursor_isNull(referenced) && is_main_file_cursor(referenced)) {
        return false;
    }
    const std::string name = lower(cx_text(clang_getCursorSpelling(
        clang_Cursor_isNull(referenced) ? cursor : referenced)));
    const std::string qualified = lower(
        clang_Cursor_isNull(referenced) ? name : qualified_name(referenced));
    return name == "fopen" || name.rfind("curl_", 0) == 0 ||
           name.rfind("sqlite3_", 0) == 0 || qualified.find("::json::") != std::string::npos ||
           qualified.find("nlohmann::json") != std::string::npos;
}

int tuple_output_count(CXCursor cursor) {
    if (clang_getCursorKind(cursor) == CXCursor_Constructor) {
        return 0;
    }
    const std::string type = cx_text(clang_getTypeSpelling(clang_getCursorResultType(cursor)));
    const auto pair_position = type.find("pair<");
    if (pair_position != std::string::npos) {
        return 2;
    }
    const auto tuple_position = type.find("tuple<");
    if (tuple_position == std::string::npos) {
        return 0;
    }

    int depth = 0;
    int count = 1;
    for (std::size_t index = tuple_position; index < type.size(); ++index) {
        if (type[index] == '<') {
            ++depth;
        } else if (type[index] == '>') {
            --depth;
            if (depth == 0) {
                break;
            }
        } else if (type[index] == ',' && depth == 1) {
            ++count;
        }
    }
    return count;
}

int signature_reducible_lines(CXCursor cursor) {
    const int start = cursor_line(cursor);
    int end = start;
    const int arguments = clang_Cursor_getNumArguments(cursor);
    for (int index = 0; index < arguments; ++index) {
        end = std::max(end, cursor_end_line(clang_Cursor_getArgument(cursor, index)));
    }
    return std::max(0, end - start);
}

int direct_method_count(CXCursor cursor) {
    int count = 0;
    clang_visitChildren(
        cursor,
        [](CXCursor child, CXCursor, CXClientData client_data) {
            if (clang_getCursorKind(child) == CXCursor_CXXMethod) {
                ++(*static_cast<int*>(client_data));
            }
            return CXChildVisit_Continue;
        },
        &count);
    return count;
}

bool has_direct_behavior(CXCursor cursor) {
    bool has_behavior = false;
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
        &has_behavior);
    return has_behavior;
}

void analyze_dependency(AnalysisState& state, CXCursor cursor) {
    const std::string include_name = cx_text(clang_getCursorSpelling(cursor));
    if (include_name.empty()) {
        return;
    }
    const auto result = dependency_result(state.source, classify_include(include_name));
    if (result.has_value()) {
        add_finding(state, cursor_line(cursor), result->code, result->message, result->severity);
    }
}

void analyze_function(AnalysisState& state, CXCursor cursor) {
    if (!is_component_role(state.source.role)) {
        return;
    }
    const int input_count = std::max(0, clang_Cursor_getNumArguments(cursor));
    const int output_count = tuple_output_count(cursor);
    const bool input_violation = input_count > 1;
    const bool output_violation = output_count > 1;
    const int line = cursor_line(cursor);

    if (input_violation) {
        if (state.reducible_lines == 0) {
            state.first_offending_line = line;
        }
        add_finding(
            state,
            line,
            "UPD301",
            "multiple inputs reduce readability; consider one Input Container",
            "attention");
    }
    if (output_violation) {
        if (state.reducible_lines == 0) {
            state.first_offending_line = line;
        }
        add_finding(
            state,
            line,
            "UPD302",
            "multiple return values reduce readability; consider one Output Container",
            "attention");
    }
    if (input_violation || output_violation) {
        const int excess = std::max(0, input_count - 1) + std::max(0, output_count - 1);
        state.reducible_lines += std::max(signature_reducible_lines(cursor), excess);
    }
}

void analyze_class(AnalysisState& state, CXCursor cursor) {
    if (!clang_isCursorDefinition(cursor)) {
        return;
    }
    if (has_direct_behavior(cursor)) {
        ++state.major_class_count;
        if (state.major_class_count == 2) {
            state.second_class_line = cursor_line(cursor);
        }
    }

    const int line = cursor_line(cursor);
    const int lines = std::max(1, cursor_end_line(cursor) - line + 1);
    const int methods = direct_method_count(cursor);
    if (lines > kMaxResponsibilityLines || methods > kMaxResponsibilityMethods) {
        const std::string name = cx_text(clang_getCursorSpelling(cursor));
        add_finding(
            state,
            line,
            "UPD401",
            "type " + name + " is too large for one responsibility (lines=" +
                std::to_string(lines) + ", methods=" + std::to_string(methods) + ")",
            "warning");
    }
}

CXChildVisitResult visit_cursor(CXCursor cursor, CXCursor, CXClientData client_data) {
    auto& state = *static_cast<AnalysisState*>(client_data);
    if (!is_main_file_cursor(cursor)) {
        return CXChildVisit_Continue;
    }

    const auto kind = clang_getCursorKind(cursor);
    if (kind == CXCursor_InclusionDirective) {
        analyze_dependency(state, cursor);
    }
    if (kind == CXCursor_ClassDecl || kind == CXCursor_StructDecl || kind == CXCursor_ClassTemplate) {
        analyze_class(state, cursor);
    }
    if (kind == CXCursor_FunctionDecl || kind == CXCursor_CXXMethod || kind == CXCursor_Constructor) {
        analyze_function(state, cursor);
    }

    if (state.source.role == "commander") {
        if (kind == CXCursor_ForStmt || kind == CXCursor_CXXForRangeStmt ||
            kind == CXCursor_WhileStmt || kind == CXCursor_DoStmt) {
            add_finding(state, cursor_line(cursor), "UPD201", "Commander loop", "warning");
        } else if (kind == CXCursor_BinaryOperator && is_arithmetic(cursor)) {
            add_finding(state, cursor_line(cursor), "UPD202", "Commander calculation", "warning");
        } else if (is_direct_work(cursor)) {
            add_finding(state, cursor_line(cursor), "UPD203", "Commander direct I/O/API call", "error");
        }
    }

    return CXChildVisit_Recurse;
}

}  // namespace

std::vector<Finding> analyze_cpp_ast(
    const std::filesystem::path& path,
    const std::filesystem::path& root,
    const std::string& relative,
    const std::vector<IgnoreRule>& rules) {
    AnalysisState state{classify_path(relative), relative, {}, &rules};
    std::ifstream source(path);
    if (!source) {
        return {Finding{relative, 1, "UPD001", "read failed", "error"}};
    }
    std::string line;
    while (std::getline(source, line)) {
        state.lines.push_back(line);
        if (line.find_first_not_of(" \t\r\n") != std::string::npos) {
            ++state.effective_lines;
        }
    }

    const std::vector<std::string> argument_storage = resolve_compile_arguments(path, root);
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
        return {Finding{relative, 1, "UPD002", "AST parse failed", "error"}};
    }

    clang_visitChildren(clang_getTranslationUnitCursor(unit), visit_cursor, &state);

    if (state.major_class_count > 1) {
        add_finding(
            state,
            state.second_class_line,
            "UPD402",
            "file contains multiple responsibility-bearing types",
            "warning");
    }
    const bool substantial_compression =
        state.reducible_lines >= kMinReducibleLines &&
        static_cast<double>(state.reducible_lines) /
                static_cast<double>(std::max(1, state.effective_lines)) >=
            kMinReductionRatio;
    if ((state.source.role == "commander" || state.source.role == "messenger") &&
        substantial_compression) {
        add_finding(
            state,
            state.first_offending_line,
            "UPD303",
            "Compresser/Container introduction is expected to substantially reduce this Commander/Messenger",
            "warning");
    }

    clang_disposeTranslationUnit(unit);
    clang_disposeIndex(index);
    return state.findings;
}

}  // namespace upd_checker
