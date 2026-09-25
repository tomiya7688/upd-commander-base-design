#include <cassert>
#include <filesystem>
#include <fstream>
#include <string>
#include <vector>

#include "classifier.hpp"
#include "dependency_rules.hpp"
#include "scanner.hpp"

namespace {

bool has_upd102(const std::string& target_path) {
    const auto source = upd_checker::classify_path(
        "applications/main/process/main_commander.cpp");
    const auto target = upd_checker::classify_include(target_path);
    const auto result = upd_checker::dependency_result(source, target);
    return result.has_value() && result->code == "UPD102";
}

void test_shared_processing_does_not_bypass_boundary() {
    assert(has_upd102(
        "applications/settings/shared/process/settings_processing.hpp"));
}

void test_shared_data_does_not_bypass_boundary() {
    assert(has_upd102("applications/settings/shared/data/storage.hpp"));
}

void test_shared_contracts_remain_boundary_api() {
    assert(!has_upd102(
        "applications/settings/shared/contracts/process/settings_processing.hpp"));
}

void test_shared_messages_remain_boundary_api() {
    assert(!has_upd102(
        "applications/settings/shared/messages/process/settings_processing.hpp"));
}

bool has_code(const std::vector<upd_checker::Finding>& findings, const std::string& code) {
    for (const auto& finding : findings) {
        if (finding.code == code) {
            return true;
        }
    }
    return false;
}

void write_file(const std::filesystem::path& path, const std::string& content) {
    std::filesystem::create_directories(path.parent_path());
    std::ofstream file(path);
    file << content;
}

void test_common_dependency_rules_and_role_suppression() {
    const auto source = upd_checker::classify_path("common/contracts/bridge.hpp");
    const auto process = upd_checker::classify_path("process/engine.hpp");
    const auto forbidden = upd_checker::dependency_result(source, process);
    assert(forbidden.has_value());
    assert(forbidden->code == "UPD101");
    assert(forbidden->severity == "error");

    const auto messenger = upd_checker::classify_path("ui/messenger/ui_messenger.cpp");
    const auto common_processing_name =
        upd_checker::classify_path("common/contracts/settings_processing.hpp");
    assert(common_processing_name.layer == "common");
    assert(common_processing_name.role == "processing");
    assert(!upd_checker::dependency_result(messenger, common_processing_name).has_value());
}

void test_resolved_common_include_and_custom_root() {
    const auto root =
        std::filesystem::temp_directory_path() / "upd_cpp_shared_resolved_include";
    std::filesystem::remove_all(root);
    write_file(root / "contracts" / "bridge.cpp",
        "#include \"process/engine.hpp\"\nvoid bridge() { ProcessEngine::run(); }\n");
    write_file(root / "process" / "engine.hpp",
        "struct ProcessEngine { static void run() {} };\n");
    write_file(root / "compile_commands.json",
        "[{\"directory\":\"" + root.generic_string() +
            "\",\"arguments\":[\"clang++\",\"-I.\",\"-c\","
            "\"contracts/bridge.cpp\",\"-o\",\"bridge.o\"],"
            "\"file\":\"contracts/bridge.cpp\"}]\n");

    const auto findings = upd_checker::scan_path(
        root.string(), {}, 2, 12, 80, 3, 2, {"contracts"});
    assert(has_code(findings, "UPD101"));
    std::filesystem::remove_all(root);
}

}  // namespace

int main() {
    test_shared_processing_does_not_bypass_boundary();
    test_shared_data_does_not_bypass_boundary();
    test_shared_contracts_remain_boundary_api();
    test_shared_messages_remain_boundary_api();
    test_common_dependency_rules_and_role_suppression();
    test_resolved_common_include_and_custom_root();
    return 0;
}
