#pragma once

#include <string>

namespace upd_checker {

// {
// 責務: [BaselineEntry: Finding identityと表示用metadataを保持する]
// フィールド: [fingerprint/rule/path/symbol/context: identity, severity/line/message: 表示情報]
// }
struct BaselineEntry {
    std::string fingerprint;
    std::string rule;
    std::string path;
    std::string symbol;
    std::string context;
    std::string severity;
    int line = 0;
    std::string message;
};

}  // namespace upd_checker
