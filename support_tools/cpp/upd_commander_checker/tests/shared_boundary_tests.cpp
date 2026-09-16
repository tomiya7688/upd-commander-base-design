#include <cassert>

#include "classifier.hpp"
#include "dependency_rules.hpp"

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

}  // namespace

int main() {
    test_shared_processing_does_not_bypass_boundary();
    test_shared_data_does_not_bypass_boundary();
    test_shared_contracts_remain_boundary_api();
    test_shared_messages_remain_boundary_api();
    return 0;
}
