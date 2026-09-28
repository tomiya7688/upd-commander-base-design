#include <cassert>
#include <array>
#include <fstream>
#include <iterator>
#include <stdexcept>
#include <string>

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

}  // namespace

int main() {
    test_shared_fingerprint_fixtures();
    test_rejects_invalid_identity();
    return 0;
}
