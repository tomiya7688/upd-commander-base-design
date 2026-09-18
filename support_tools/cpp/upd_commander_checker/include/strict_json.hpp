#pragma once

#include <map>
#include <string>
#include <vector>

namespace upd_checker {

struct JsonValue {
    enum class Type {
        null_value,
        boolean,
        number,
        string,
        array,
        object,
    };

    Type type = Type::null_value;
    bool boolean_value = false;
    std::string number_value;
    std::string string_value;
    std::vector<JsonValue> array_value;
    std::map<std::string, JsonValue> object_value;
};

JsonValue parse_json(const std::string& text);

}  // namespace upd_checker
