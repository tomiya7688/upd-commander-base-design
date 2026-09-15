#include <cassert>
#include <filesystem>
#include <fstream>
#include <string>
#include <vector>

#include "report_output.hpp"

namespace {

void test_parent_creation_failure() {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_output_parent";
    std::filesystem::remove_all(root);
    std::filesystem::create_directories(root);
    const auto blocker = root / "blocker";
    std::ofstream(blocker) << "file";

    const int code = upd_checker::finish_report(
        {"OK"},
        (blocker / "report.txt").string(),
        0);
    assert(code == 2);
    std::filesystem::remove_all(root);
}

void test_directory_output_failure() {
    const auto root = std::filesystem::temp_directory_path() / "upd_cpp_output_directory";
    std::filesystem::remove_all(root);
    const auto output = root / "report";
    std::filesystem::create_directories(output);

    const int code = upd_checker::finish_report(
        {"FAIL e=1 w=0 a=0"},
        output.string(),
        1);
    assert(code == 2);
    std::filesystem::remove_all(root);
}

}  // namespace

int main() {
    test_parent_creation_failure();
    test_directory_output_failure();
    return 0;
}
