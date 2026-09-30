#ifdef NDEBUG
#undef NDEBUG
#endif
#include <cassert>
#include <array>
#include <filesystem>
#include <fstream>
#include <iterator>
#include <stdexcept>
#include <string>
#include <chrono>

#include "baseline.hpp"
#include "strict_json.hpp"

namespace {

const upd_checker::JsonValue& required(
    const upd_checker::JsonValue& value, const std::string& key) {
    const auto found = value.object_value.find(key);
    assert(found != value.object_value.end());
    return found->second;
}

std::string string_value(const upd_checker::JsonValue& value) {
    assert(value.type == upd_checker::JsonValue::Type::string);
    return value.string_value;
}

void test_shared_fingerprint_fixtures() {
    std::ifstream file(BASELINE_FIXTURE_PATH);
    assert(file.is_open());
    const std::string text(
        (std::istreambuf_iterator<char>(file)), std::istreambuf_iterator<char>());
    const auto fixture = upd_checker::parse_json(text);
    const auto& vectors = required(fixture, "vectors");
    assert(vectors.type == upd_checker::JsonValue::Type::array);
    for (const auto& vector : vectors.array_value) {
        const auto& finding = required(vector, "finding");
        const auto actual = upd_checker::finding_fingerprint(
            string_value(required(finding, "rule")),
            string_value(required(finding, "path")),
            string_value(required(finding, "symbol")),
            string_value(required(finding, "context")));
        assert(actual == string_value(required(vector, "expected_fingerprint")));
    }
}

void test_rejects_invalid_identity() {
    for (const auto& fields : {
             std::array<std::string, 4>{"UPD101", "../outside.cpp", "", "target=x"},
             std::array<std::string, 4>{"UPD101", "C:/outside.cpp", "", "target=x"},
             std::array<std::string, 4>{"UPD1", "src/file.cpp", "", "target=x"},
             std::array<std::string, 4>{"UPD101", "src/file.cpp", "", ""}}) {
        bool rejected = false;
        try {
            (void)upd_checker::finding_fingerprint(fields[0], fields[1], fields[2], fields[3]);
        } catch (const std::invalid_argument&) {
            rejected = true;
        }
        assert(rejected);
    }
}

void test_baseline_round_trip_and_classification() {
    const auto root = std::filesystem::temp_directory_path() /
        ("upd-baseline-test-" + std::to_string(
            std::chrono::steady_clock::now().time_since_epoch().count()));
    const auto filename = root / "nested" / "baseline.json";
    upd_checker::BaselineEntry old_entry;
    old_entry.rule = "UPD101";
    old_entry.path = "src/a.cpp";
    old_entry.symbol = "Ui.Screen.Run";
    old_entry.context = "target=data.store";
    old_entry.severity = "warning";
    old_entry.line = 5;
    old_entry.message = "old message with \"quotes\"\nand newline";
    upd_checker::write_baseline(filename.string(), {old_entry});

    const auto baseline = upd_checker::load_baseline(filename.string());
    assert(baseline.findings.size() == 1);
    assert(baseline.findings.front().message == old_entry.message);
    auto current_entry = old_entry;
    current_entry.line = 44;
    current_entry.message = "updated display text";
    upd_checker::BaselineEntry new_entry = old_entry;
    new_entry.rule = "UPD102";
    new_entry.context = "target=other";
    const auto comparison = upd_checker::compare_baseline({current_entry, new_entry}, baseline);
    assert(comparison.existing.size() == 1);
    assert(comparison.new_findings.size() == 1);
    assert(comparison.resolved.empty());

    const auto resolved = upd_checker::compare_baseline({}, baseline);
    assert(resolved.resolved.size() == 1);
    std::filesystem::remove_all(root);
}

void test_rejects_invalid_baseline_documents() {
    for (const auto& input : {
             "{}",
             "{\"schema_version\":2,\"fingerprint_version\":1,\"findings\":[]}",
             "{\"schema_version\":1,\"fingerprint_version\":1,\"findings\":null}",
             "{\"schema_version\":1,\"fingerprint_version\":1,\"findings\":[{}]}",
             "{\"schema_version\":1,\"fingerprint_version\":1,\"findings\":[]} {}}"}) {
        bool rejected = false;
        try {
            (void)upd_checker::validate_baseline(upd_checker::parse_json(input));
        } catch (const std::exception&) {
            rejected = true;
        }
        assert(rejected);
    }
}

}  // namespace

int main() {
    test_shared_fingerprint_fixtures();
    test_rejects_invalid_identity();
    test_baseline_round_trip_and_classification();
    test_rejects_invalid_baseline_documents();
    return 0;
}
