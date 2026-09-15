#pragma once

#include <clang-c/Index.h>

namespace upd_checker {

int first_main_file_error_line(CXTranslationUnit unit);

}  // namespace upd_checker
