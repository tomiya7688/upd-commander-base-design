#pragma once

#include <string>
#include <vector>

#include "baseline_document.hpp"
#include "classified_baseline.hpp"
#include "baseline_entry.hpp"
#include "strict_json.hpp"

namespace upd_checker {

// {
// 責務: [finding_fingerprint: 共通v1契約に基づくFinding identityを生成する]
// 処理: [1: identity fieldを正規化する, 2: NUL区切りSHA-256を計算する]
// 引数: [rule/path/symbol/context: Finding identityを構成する値]
// 戻り値: [sha256:形式のfingerprint]
// エラー: [fieldがidentity契約に反する場合]
// }
std::string finding_fingerprint(
    const std::string& rule,
    const std::string& path,
    const std::string& symbol,
    const std::string& context);

// {
// 責務: [build_baseline: Finding一覧から決定的なbaselineを構築する]
// 処理: [1: entryを正規化する, 2: fingerprint重複を拒否する, 3:順序を固定する]
// 引数: [findings: 記録するFinding一覧]
// 戻り値: [version 1 Baseline]
// }
Baseline build_baseline(const std::vector<BaselineEntry>& findings);
// {
// 責務: [validate_baseline: JSON値をschema v1として検証する]
// 処理: [1: versionと必須fieldを確認する, 2: entry identityを再計算する]
// 引数: [document: 読み取ったbaseline JSON値]
// 戻り値: [検証済みBaseline]
// エラー: [schema・field・fingerprintが不正な場合]
// }
Baseline validate_baseline(const JsonValue& document);
// {
// 責務: [load_baseline: baselineファイルを読み込みschemaを検証する]
// 処理: [1: JSONを読む, 2: baseline v1として検証する]
// 引数: [filename: 読み込むファイル]
// 戻り値: [検証済みBaseline]
// エラー: [読込または内容検証に失敗した場合]
// }
Baseline load_baseline(const std::string& filename);
// {
// 責務: [write_baseline: Finding一覧をJSON baselineとして保存する]
// 処理: [1: baselineを構築する, 2: 親directoryを作る, 3: JSONを書き込む]
// 引数: [filename: 保存先, findings: 保存するFinding一覧]
// 戻り値: [なし]
// 副作用: [必要なdirectoryとbaselineファイルを作成する]
// エラー: [構築または書込に失敗した場合]
// }
void write_baseline(const std::string& filename, const std::vector<BaselineEntry>& findings);
// {
// 責務: [compare_baseline: 現在とbaselineのFindingを差分分類する]
// 処理: [1: 両方のidentityを検証する, 2: fingerprint集合を比較する]
// 引数: [current: 現在のFinding, baseline: 比較対象snapshot]
// 戻り値: [NEW/EXISTING/RESOLVEDごとのFinding]
// エラー: [baselineまたは現在のFindingに重複・不正identityがある場合]
// }
ClassifiedBaseline compare_baseline(
    const std::vector<BaselineEntry>& current,
    const Baseline& baseline);

}  // namespace upd_checker
