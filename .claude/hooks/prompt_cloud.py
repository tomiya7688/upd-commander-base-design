"""クラウド用UserPromptSubmitフック: 単独の「次」を共通の読み替え指示に展開する（個人情報・E:依存なし）。"""

import json
import sys

NEXT_WORDS = {"次", "つぎ", "次へ", "next", "n"}
NEXT_TEXT = (
    "[自動読み替え] ユーザーの「次」「次へ」など同じ意味の継続指示は常に、現在の目標に沿う設計または実装をキリのいい所（検証可能な一区切り）まで進める実行指示として扱う。"
    "目標・会話・計画・Issue・依存関係と最新の共有ブランチ/作業状態を確認し、他セッションの進捗を読み直してから次の単位を選ぶ。共有の進捗記録がある場合は着手前に担当単位を記録する。"
    "確認だけで終わらず、必要な調査・整形・検証・レビューを行って具体的な設計または実装の成果を作る。確認が必要なのは破壊的操作・課金・秘密/権限変更・必須レビューやCIの迂回など既存の境界に限る。"
    "完了後は成果・確認結果・未完了事項や障害を短く報告する。"
)


# {"責務":{"main":"「次」だけの入力に読み替え指示を添える"},
#  "処理":["入力JSONを読む","promptが「次」類なら追加文脈を出力する"],
#  "引数":[],"戻り値":[],"副作用":{"stdout":"追加文脈のJSON"}}
def main():
    try:
        data = json.loads(sys.stdin.buffer.read().decode("utf-8", "replace"))
    except ValueError:
        return
    if str(data.get("prompt", "")).strip().lower() in NEXT_WORDS:
        out = {
            "hookSpecificOutput": {
                "hookEventName": "UserPromptSubmit",
                "additionalContext": NEXT_TEXT,
            }
        }
        print(json.dumps(out, ensure_ascii=False))


main()
