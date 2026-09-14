# Responsibility Check Rules

この文書は、UPD Commander Checker が責務過多を判定するときの共通基準を定義します。

## UPD401: 責務単位が過大

`UPD401` は、1つの責務単位が大きくなりすぎて分割候補になっていることを示す `warning` です。

これは「250行を超えたら必ず複数責務である」と断定する規則ではありません。UPD Commander 設計では大きな責務単位を分割候補として早めに可視化できることを重視するため、保守的な警告境界として扱います。

### 共通しきい値

- source span: **250行まで**
- responsibility-bearing methods: **12個まで**
- **251行以上**、または **13メソッド以上**で `UPD401` warning

行数とメソッド数のどちらか一方が境界を超えれば警告対象です。

### 責務単位

ASTで型を認識できる実装は、ファイル全体より型を優先して判定します。

- Python: class
- Go: named type + receiver methods を1責務単位として集約
- C++: class / struct
- C#: class / struct / record などの type declaration

言語構造上、責務単位を型として確定できないソースだけは file/module を近似責務単位として扱って構いません。この場合、メッセージに file/module approximation であることが分かる表現を用います。

### 行数の数え方

ASTで責務単位の開始行と終了行を取得できる場合は、その source span を使用します。空行やコメントだけを言語ごとに別計算して基準を変えてはいけません。

Goではメソッドが型宣言の外側に書かれるため、named type declaration の source span と、その型をreceiverに持つ各method declarationのsource spanを合計します。

### メソッド数の数え方

責務単位へ直接所属する通常のmethod declarationを数えます。

- コンストラクタは含めない
- property/accessor はmethod数へ含めない
- nested type 内部のmethodは外側typeへ加算しない

言語に通常のmethod概念がない場合のみ、最も近い callable member を使用できます。

## UPD402との関係

`UPD401` は「1責務単位そのものが過大」を示します。

`UPD402` は「1ファイルに複数の責務を持つ主要type/classが存在する」を示します。

同じファイルで両方成立する場合は、両方を報告して構いません。

## Checker実装要件

各言語Checkerは、この文書の250行 / 12メソッドを共通の基準値として実装しなければなりません。

しきい値を変更する場合は、先にこの文書を変更し、同一変更で全言語実装と境界テストを同期します。
