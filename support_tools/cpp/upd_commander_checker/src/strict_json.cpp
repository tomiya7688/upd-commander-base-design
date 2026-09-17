#include "strict_json.hpp"

#include <cctype>
#include <cstdint>
#include <stdexcept>
#include <string>

namespace upd_checker {
namespace {

struct Cursor {
    const std::string& text;
    std::size_t position = 0;
};

[[noreturn]] void fail() {
    throw std::invalid_argument("invalid JSON");
}

void skip_whitespace(Cursor& cursor) {
    while (cursor.position < cursor.text.size()) {
        const unsigned char ch = static_cast<unsigned char>(cursor.text[cursor.position]);
        if (ch != ' ' && ch != '\t' && ch != '\r' && ch != '\n') {
            break;
        }
        ++cursor.position;
    }
}

bool consume(Cursor& cursor, char expected) {
    if (cursor.position >= cursor.text.size() || cursor.text[cursor.position] != expected) {
        return false;
    }
    ++cursor.position;
    return true;
}

void expect(Cursor& cursor, char expected) {
    if (!consume(cursor, expected)) {
        fail();
    }
}

int hex_value(char ch) {
    if (ch >= '0' && ch <= '9') {
        return ch - '0';
    }
    if (ch >= 'a' && ch <= 'f') {
        return 10 + ch - 'a';
    }
    if (ch >= 'A' && ch <= 'F') {
        return 10 + ch - 'A';
    }
    return -1;
}

std::uint32_t parse_hex_quad(Cursor& cursor) {
    std::uint32_t value = 0;
    for (int index = 0; index < 4; ++index) {
        if (cursor.position >= cursor.text.size()) {
            fail();
        }
        const int digit = hex_value(cursor.text[cursor.position++]);
        if (digit < 0) {
            fail();
        }
        value = (value << 4U) | static_cast<std::uint32_t>(digit);
    }
    return value;
}

void append_utf8(std::string& output, std::uint32_t code_point) {
    if (code_point <= 0x7FU) {
        output.push_back(static_cast<char>(code_point));
    } else if (code_point <= 0x7FFU) {
        output.push_back(static_cast<char>(0xC0U | (code_point >> 6U)));
        output.push_back(static_cast<char>(0x80U | (code_point & 0x3FU)));
    } else if (code_point <= 0xFFFFU) {
        output.push_back(static_cast<char>(0xE0U | (code_point >> 12U)));
        output.push_back(static_cast<char>(0x80U | ((code_point >> 6U) & 0x3FU)));
        output.push_back(static_cast<char>(0x80U | (code_point & 0x3FU)));
    } else if (code_point <= 0x10FFFFU) {
        output.push_back(static_cast<char>(0xF0U | (code_point >> 18U)));
        output.push_back(static_cast<char>(0x80U | ((code_point >> 12U) & 0x3FU)));
        output.push_back(static_cast<char>(0x80U | ((code_point >> 6U) & 0x3FU)));
        output.push_back(static_cast<char>(0x80U | (code_point & 0x3FU)));
    } else {
        fail();
    }
}

std::uint32_t parse_unicode_escape(Cursor& cursor) {
    std::uint32_t code_point = parse_hex_quad(cursor);
    if (code_point >= 0xD800U && code_point <= 0xDBFFU) {
        if (cursor.position + 2 > cursor.text.size() ||
            cursor.text[cursor.position] != '\\' ||
            cursor.text[cursor.position + 1] != 'u') {
            fail();
        }
        cursor.position += 2;
        const std::uint32_t low = parse_hex_quad(cursor);
        if (low < 0xDC00U || low > 0xDFFFU) {
            fail();
        }
        code_point = 0x10000U + ((code_point - 0xD800U) << 10U) + (low - 0xDC00U);
    } else if (code_point >= 0xDC00U && code_point <= 0xDFFFU) {
        fail();
    }
    return code_point;
}

std::string parse_string(Cursor& cursor) {
    expect(cursor, '"');
    std::string output;
    while (cursor.position < cursor.text.size()) {
        const unsigned char raw = static_cast<unsigned char>(cursor.text[cursor.position++]);
        if (raw == '"') {
            return output;
        }
        if (raw < 0x20U) {
            fail();
        }
        if (raw != '\\') {
            output.push_back(static_cast<char>(raw));
            continue;
        }
        if (cursor.position >= cursor.text.size()) {
            fail();
        }
        const char escaped = cursor.text[cursor.position++];
        switch (escaped) {
            case '"': output.push_back('"'); break;
            case '\\': output.push_back('\\'); break;
            case '/': output.push_back('/'); break;
            case 'b': output.push_back('\b'); break;
            case 'f': output.push_back('\f'); break;
            case 'n': output.push_back('\n'); break;
            case 'r': output.push_back('\r'); break;
            case 't': output.push_back('\t'); break;
            case 'u': append_utf8(output, parse_unicode_escape(cursor)); break;
            default: fail();
        }
    }
    fail();
}

void consume_digits(Cursor& cursor) {
    if (cursor.position >= cursor.text.size() ||
        !std::isdigit(static_cast<unsigned char>(cursor.text[cursor.position]))) {
        fail();
    }
    while (cursor.position < cursor.text.size() &&
           std::isdigit(static_cast<unsigned char>(cursor.text[cursor.position]))) {
        ++cursor.position;
    }
}

void parse_number(Cursor& cursor) {
    consume(cursor, '-');
    if (cursor.position >= cursor.text.size()) {
        fail();
    }
    if (cursor.text[cursor.position] == '0') {
        ++cursor.position;
        if (cursor.position < cursor.text.size() &&
            std::isdigit(static_cast<unsigned char>(cursor.text[cursor.position]))) {
            fail();
        }
    } else {
        if (cursor.text[cursor.position] < '1' || cursor.text[cursor.position] > '9') {
            fail();
        }
        consume_digits(cursor);
    }
    if (consume(cursor, '.')) {
        consume_digits(cursor);
    }
    if (cursor.position < cursor.text.size() &&
        (cursor.text[cursor.position] == 'e' || cursor.text[cursor.position] == 'E')) {
        ++cursor.position;
        if (cursor.position < cursor.text.size() &&
            (cursor.text[cursor.position] == '+' || cursor.text[cursor.position] == '-')) {
            ++cursor.position;
        }
        consume_digits(cursor);
    }
}

void parse_literal(Cursor& cursor, const std::string& literal) {
    if (cursor.text.compare(cursor.position, literal.size(), literal) != 0) {
        fail();
    }
    cursor.position += literal.size();
}

JsonValue parse_value(Cursor& cursor);

JsonValue parse_array(Cursor& cursor) {
    JsonValue value;
    value.type = JsonValue::Type::array;
    expect(cursor, '[');
    skip_whitespace(cursor);
    if (consume(cursor, ']')) {
        return value;
    }
    while (true) {
        value.array_value.push_back(parse_value(cursor));
        skip_whitespace(cursor);
        if (consume(cursor, ']')) {
            return value;
        }
        expect(cursor, ',');
        skip_whitespace(cursor);
    }
}

JsonValue parse_object(Cursor& cursor) {
    JsonValue value;
    value.type = JsonValue::Type::object;
    expect(cursor, '{');
    skip_whitespace(cursor);
    if (consume(cursor, '}')) {
        return value;
    }
    while (true) {
        if (cursor.position >= cursor.text.size() || cursor.text[cursor.position] != '"') {
            fail();
        }
        const std::string key = parse_string(cursor);
        skip_whitespace(cursor);
        expect(cursor, ':');
        skip_whitespace(cursor);
        value.object_value.insert_or_assign(key, parse_value(cursor));
        skip_whitespace(cursor);
        if (consume(cursor, '}')) {
            return value;
        }
        expect(cursor, ',');
        skip_whitespace(cursor);
    }
}

JsonValue parse_value(Cursor& cursor) {
    skip_whitespace(cursor);
    if (cursor.position >= cursor.text.size()) {
        fail();
    }
    const char ch = cursor.text[cursor.position];
    if (ch == '{') {
        return parse_object(cursor);
    }
    if (ch == '[') {
        return parse_array(cursor);
    }
    JsonValue value;
    if (ch == '"') {
        value.type = JsonValue::Type::string;
        value.string_value = parse_string(cursor);
        return value;
    }
    if (ch == 't') {
        parse_literal(cursor, "true");
        value.type = JsonValue::Type::boolean;
        value.boolean_value = true;
        return value;
    }
    if (ch == 'f') {
        parse_literal(cursor, "false");
        value.type = JsonValue::Type::boolean;
        value.boolean_value = false;
        return value;
    }
    if (ch == 'n') {
        parse_literal(cursor, "null");
        return value;
    }
    if (ch == '-' || (ch >= '0' && ch <= '9')) {
        parse_number(cursor);
        value.type = JsonValue::Type::number;
        return value;
    }
    fail();
}

}  // namespace

JsonValue parse_json(const std::string& text) {
    Cursor cursor{text};
    JsonValue value = parse_value(cursor);
    skip_whitespace(cursor);
    if (cursor.position != text.size()) {
        fail();
    }
    return value;
}

}  // namespace upd_checker
