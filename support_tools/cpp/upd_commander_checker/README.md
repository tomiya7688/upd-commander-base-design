# C++ UPD Commander Checker

C++プロジェクト向けのUPD Commander静的チェッカーです。

標準C++17のみで実装し、UPD Commanderの依存規則・Application境界・Commander責務を軽量に検査します。完全なC++構文検査は通常のコンパイラへ任せます。

## ビルド

Windows:

```bat
build_exe.bat
```

または:

```bash
cmake -S . -B build -DCMAKE_BUILD_TYPE=Release
cmake --build build --config Release --target upd-commander-check
```

## 実行

```bash
upd-commander-check path/to/project
```

出力:

```text
E UPD102 applications/main/process/main_commander.cpp:3 cross-application internal dependency
W UPD202 process/game_commander.cpp:12 Commander calculation
FAIL e=1 w=1
```

問題なし:

```text
OK
```

## Ignore

```bash
upd-commander-check --ignore "tests/**" --ignore "generated/**" .
```

`.updcommanderignore`:

```text
generated/**
UPD202 process/fast_commander.cpp # performance hot path
```

行単位:

```cpp
value = left + right; // upd: ignore UPD202 - performance hot path
```

## Application境界

`app/`, `apps/`, `application/`, `applications/`, `feature/`, `features/` の直下をApplication IDとして扱います。

別Applicationの内部実装への直接 `#include` は `UPD102` です。Messenger、contract/contracts、dto/dtos、shared は境界APIとして許可します。

## 規則

- `UPD001`: ソース読み込み失敗
- `UPD101`: UI / Process / Data・Commander / Messenger / Processing依存違反
- `UPD102`: Application境界越しの内部実装直接依存
- `UPD201`: Commander内のループ
- `UPD202`: Commander内の計算式（軽量ヒューリスティック）
- `UPD203`: Commander内の直接I/O/API呼び出し（軽量ヒューリスティック）

## テスト

```bash
cmake -S . -B build
cmake --build build
ctest --test-dir build --output-on-failure
```
