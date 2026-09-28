#include "baseline.hpp"

#include <algorithm>
#include <array>
#include <cctype>
#include <cstdint>
#include <iomanip>
#include <sstream>
#include <stdexcept>
#include <string>
#include <vector>

#ifdef _WIN32
#define NOMINMAX
#include <windows.h>
#else
#include <unicode/unorm2.h>
#include <unicode/ustring.h>
#endif

namespace upd_checker {
namespace {

std::uint32_t rotate_right(std::uint32_t value, int amount) {
    return (value >> amount) | (value << (32 - amount));
}

std::string normalize_nfc(const std::string& input) {
#ifdef _WIN32
    const int wide_length = MultiByteToWideChar(
        CP_UTF8, MB_ERR_INVALID_CHARS, input.data(), static_cast<int>(input.size()), nullptr, 0);
    if (wide_length == 0 && !input.empty()) {
        throw std::invalid_argument("identity field is not valid UTF-8");
    }
    std::wstring wide(static_cast<std::size_t>(wide_length), L'\0');
    if (wide_length > 0) {
        MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, input.data(),
            static_cast<int>(input.size()), wide.data(), wide_length);
    }
    const int normalized_length = NormalizeString(
        NormalizationC, wide.data(), wide_length, nullptr, 0);
    if (normalized_length <= 0 && wide_length != 0) {
        throw std::invalid_argument("Unicode NFC normalization failed");
    }
    std::wstring normalized(static_cast<std::size_t>(normalized_length), L'\0');
    if (normalized_length > 0) {
        NormalizeString(NormalizationC, wide.data(), wide_length, normalized.data(), normalized_length);
    }
    const int utf8_length = WideCharToMultiByte(CP_UTF8, WC_ERR_INVALID_CHARS,
        normalized.data(), normalized_length, nullptr, 0, nullptr, nullptr);
    if (utf8_length == 0 && normalized_length != 0) {
        throw std::invalid_argument("Unicode UTF-8 conversion failed");
    }
    std::string result(static_cast<std::size_t>(utf8_length), '\0');
    if (utf8_length > 0) {
        WideCharToMultiByte(CP_UTF8, WC_ERR_INVALID_CHARS, normalized.data(), normalized_length,
            result.data(), utf8_length, nullptr, nullptr);
    }
    return result;
#else
    UErrorCode status = U_ZERO_ERROR;
    int32_t utf16_length = 0;
    u_strFromUTF8(nullptr, 0, &utf16_length, input.data(), static_cast<int32_t>(input.size()), &status);
    if (status != U_BUFFER_OVERFLOW_ERROR && U_FAILURE(status)) {
        throw std::invalid_argument("identity field is not valid UTF-8");
    }
    status = U_ZERO_ERROR;
    std::vector<UChar> utf16(static_cast<std::size_t>(utf16_length) + 1);
    u_strFromUTF8(utf16.data(), static_cast<int32_t>(utf16.size()), nullptr,
        input.data(), static_cast<int32_t>(input.size()), &status);
    if (U_FAILURE(status)) {
        throw std::invalid_argument("identity field is not valid UTF-8");
    }
    const UNormalizer2* normalizer = unorm2_getNFCInstance(&status);
    if (U_FAILURE(status)) {
        throw std::runtime_error("Unicode NFC normalizer is unavailable");
    }
    status = U_ZERO_ERROR;
    const int32_t normalized_length = unorm2_normalize(
        normalizer, utf16.data(), utf16_length, nullptr, 0, &status);
    if (status != U_BUFFER_OVERFLOW_ERROR && U_FAILURE(status)) {
        throw std::invalid_argument("Unicode NFC normalization failed");
    }
    status = U_ZERO_ERROR;
    std::vector<UChar> normalized(static_cast<std::size_t>(normalized_length) + 1);
    unorm2_normalize(normalizer, utf16.data(), utf16_length, normalized.data(),
        static_cast<int32_t>(normalized.size()), &status);
    if (U_FAILURE(status)) {
        throw std::invalid_argument("Unicode NFC normalization failed");
    }
    status = U_ZERO_ERROR;
    int32_t utf8_length = 0;
    u_strToUTF8(nullptr, 0, &utf8_length, normalized.data(), normalized_length, &status);
    if (status != U_BUFFER_OVERFLOW_ERROR && U_FAILURE(status)) {
        throw std::invalid_argument("Unicode UTF-8 conversion failed");
    }
    status = U_ZERO_ERROR;
    std::string result(static_cast<std::size_t>(utf8_length), '\0');
    u_strToUTF8(result.data(), utf8_length, nullptr, normalized.data(), normalized_length, &status);
    if (U_FAILURE(status)) {
        throw std::invalid_argument("Unicode UTF-8 conversion failed");
    }
    return result;
#endif
}

std::string canonical_rule(const std::string& input) {
    std::string rule = normalize_nfc(input);
    std::transform(rule.begin(), rule.end(), rule.begin(), [](unsigned char ch) {
        return ch >= 'a' && ch <= 'z' ? static_cast<char>(ch - 'a' + 'A') : static_cast<char>(ch);
    });
    if (rule.size() < 6 || rule.compare(0, 3, "UPD") != 0 ||
        !std::all_of(rule.begin() + 3, rule.end(), [](unsigned char ch) { return ch >= '0' && ch <= '9'; })) {
        throw std::invalid_argument("rule must match UPD followed by at least three digits");
    }
    return rule;
}

std::string canonical_path(const std::string& input) {
    std::string path = normalize_nfc(input);
    std::replace(path.begin(), path.end(), '\\', '/');
    if (path.empty() || path.front() == '/' || path.rfind("//", 0) == 0 ||
        (path.size() >= 2 && std::isalpha(static_cast<unsigned char>(path[0])) && path[1] == ':')) {
        throw std::invalid_argument("path must be repository-relative");
    }
    std::vector<std::string> parts;
    std::size_t start = 0;
    while (start <= path.size()) {
        const std::size_t end = path.find('/', start);
        const std::string part = path.substr(start, end == std::string::npos ? end : end - start);
        if (part == "..") {
            throw std::invalid_argument("path must not contain '..'");
        }
        if (!part.empty() && part != ".") {
            parts.push_back(part);
        }
        if (end == std::string::npos) break;
        start = end + 1;
    }
    if (parts.empty()) {
        throw std::invalid_argument("path must not be empty");
    }
    std::string result;
    for (const auto& part : parts) {
        if (!result.empty()) result += '/';
        result += part;
    }
    return result;
}

constexpr std::array<std::uint32_t, 64> k_round = {
    0x428a2f98,0x71374491,0xb5c0fbcf,0xe9b5dba5,0x3956c25b,0x59f111f1,0x923f82a4,0xab1c5ed5,
    0xd807aa98,0x12835b01,0x243185be,0x550c7dc3,0x72be5d74,0x80deb1fe,0x9bdc06a7,0xc19bf174,
    0xe49b69c1,0xefbe4786,0x0fc19dc6,0x240ca1cc,0x2de92c6f,0x4a7484aa,0x5cb0a9dc,0x76f988da,
    0x983e5152,0xa831c66d,0xb00327c8,0xbf597fc7,0xc6e00bf3,0xd5a79147,0x06ca6351,0x14292967,
    0x27b70a85,0x2e1b2138,0x4d2c6dfc,0x53380d13,0x650a7354,0x766a0abb,0x81c2c92e,0x92722c85,
    0xa2bfe8a1,0xa81a664b,0xc24b8b70,0xc76c51a3,0xd192e819,0xd6990624,0xf40e3585,0x106aa070,
    0x19a4c116,0x1e376c08,0x2748774c,0x34b0bcb5,0x391c0cb3,0x4ed8aa4a,0x5b9cca4f,0x682e6ff3,
    0x748f82ee,0x78a5636f,0x84c87814,0x8cc70208,0x90befffa,0xa4506ceb,0xbef9a3f7,0xc67178f2};

std::string sha256(const std::string& input) {
    std::vector<std::uint8_t> data(input.begin(), input.end());
    const std::uint64_t bit_length = static_cast<std::uint64_t>(data.size()) * 8;
    data.push_back(0x80);
    while (data.size() % 64 != 56) data.push_back(0);
    for (int shift = 56; shift >= 0; shift -= 8) data.push_back(static_cast<std::uint8_t>(bit_length >> shift));
    std::array<std::uint32_t, 8> state = {0x6a09e667,0xbb67ae85,0x3c6ef372,0xa54ff53a,0x510e527f,0x9b05688c,0x1f83d9ab,0x5be0cd19};
    for (std::size_t offset = 0; offset < data.size(); offset += 64) {
        std::array<std::uint32_t, 64> words{};
        for (int i = 0; i < 16; ++i) {
            const std::size_t index = offset + static_cast<std::size_t>(i) * 4;
            words[i] = (static_cast<std::uint32_t>(data[index]) << 24) | (static_cast<std::uint32_t>(data[index+1]) << 16) |
                (static_cast<std::uint32_t>(data[index+2]) << 8) | data[index+3];
        }
        for (int i = 16; i < 64; ++i) {
            const auto s0 = rotate_right(words[i-15], 7) ^ rotate_right(words[i-15], 18) ^ (words[i-15] >> 3);
            const auto s1 = rotate_right(words[i-2], 17) ^ rotate_right(words[i-2], 19) ^ (words[i-2] >> 10);
            words[i] = words[i-16] + s0 + words[i-7] + s1;
        }
        auto a=state[0], b=state[1], c=state[2], d=state[3], e=state[4], f=state[5], g=state[6], h=state[7];
        for (int i = 0; i < 64; ++i) {
            const auto s1 = rotate_right(e,6) ^ rotate_right(e,11) ^ rotate_right(e,25);
            const auto choice = (e & f) ^ (~e & g);
            const auto temp1 = h + s1 + choice + k_round[i] + words[i];
            const auto s0 = rotate_right(a,2) ^ rotate_right(a,13) ^ rotate_right(a,22);
            const auto majority = (a & b) ^ (a & c) ^ (b & c);
            const auto temp2 = s0 + majority;
            h=g; g=f; f=e; e=d+temp1; d=c; c=b; b=a; a=temp1+temp2;
        }
        state[0]+=a; state[1]+=b; state[2]+=c; state[3]+=d; state[4]+=e; state[5]+=f; state[6]+=g; state[7]+=h;
    }
    std::ostringstream result;
    result << std::hex << std::setfill('0');
    for (const auto word : state) result << std::setw(8) << word;
    return result.str();
}

}  // namespace

std::string finding_fingerprint(
    const std::string& rule, const std::string& path,
    const std::string& symbol, const std::string& context) {
    const std::string canonical_context = normalize_nfc(context);
    if (canonical_context.empty()) {
        throw std::invalid_argument("context must not be empty");
    }
    const std::string canonical_symbol = normalize_nfc(symbol);
    const std::string fields[] = {
        "upd-finding-fingerprint-v1", canonical_rule(rule), canonical_path(path),
        canonical_symbol, canonical_context};
    std::string payload;
    for (const auto& field : fields) {
        if (field.find('\0') != std::string::npos) {
            throw std::invalid_argument("identity fields must not contain NUL");
        }
        if (!payload.empty()) payload.push_back('\0');
        payload += field;
    }
    return "sha256:" + sha256(payload);
}

}  // namespace upd_checker
