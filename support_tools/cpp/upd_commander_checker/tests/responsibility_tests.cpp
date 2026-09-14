#include <cassert>
#include <filesystem>
#include <fstream>
#include <string>

#include "scanner.hpp"

int main() {
    const auto root = std::filesystem::temp_directory_path() / "upd_checker_cpp_responsibility";
    std::filesystem::remove_all(root);
    std::filesystem::create_directories(root);

    const auto path = root / "large.cpp";
    std::ofstream file(path);
    for (int index = 0; index < 360; ++index) {
        file << "int value_" << index << " = " << index << ";\n";
    }
    file.close();

    const auto findings = upd_checker::scan_path(root.string(), {});
    bool found = false;
    for (const auto& finding : findings) {
        if (finding.code == "UPD401") {
            found = true;
            break;
        }
    }
    assert(found);
    std::filesystem::remove_all(root);
    return 0;
}
