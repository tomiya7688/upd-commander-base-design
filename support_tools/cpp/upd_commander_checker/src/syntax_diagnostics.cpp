#include "syntax_diagnostics.hpp"

#include <string>

namespace upd_checker {
namespace {

std::string cx_text(CXString value) {
    const char* raw = clang_getCString(value);
    std::string result = raw == nullptr ? std::string{} : std::string(raw);
    clang_disposeString(value);
    return result;
}

bool is_parse_error(CXDiagnostic diagnostic) {
    const CXDiagnosticSeverity severity = clang_getDiagnosticSeverity(diagnostic);
    if (severity != CXDiagnostic_Error && severity != CXDiagnostic_Fatal) {
        return false;
    }
    return cx_text(clang_getDiagnosticCategoryText(diagnostic)) == "Parse Issue";
}

}  // namespace

int first_main_file_error_line(CXTranslationUnit unit) {
    const unsigned count = clang_getNumDiagnostics(unit);
    for (unsigned index = 0; index < count; ++index) {
        CXDiagnostic diagnostic = clang_getDiagnostic(unit, index);
        if (!is_parse_error(diagnostic)) {
            clang_disposeDiagnostic(diagnostic);
            continue;
        }

        const CXSourceLocation location = clang_getDiagnosticLocation(diagnostic);
        if (clang_Location_isFromMainFile(location) == 0) {
            clang_disposeDiagnostic(diagnostic);
            continue;
        }

        unsigned line = 1;
        clang_getSpellingLocation(location, nullptr, &line, nullptr, nullptr);
        clang_disposeDiagnostic(diagnostic);
        return static_cast<int>(line == 0 ? 1 : line);
    }
    return 0;
}

}  // namespace upd_checker
