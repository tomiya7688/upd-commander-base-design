# Data Flow

## UI入力から処理まで

```text
User Input
  ↓
UI Commander
  ↓
UI Processing (必要ならUI側の入力解釈)
  ↓
UI Messenger
  ↓
Process Messenger
  ↓
Process Commander
  ↓
Process Processing
```

## Process が Data を必要とする場合

```text
Process Processing
  ↓ 必要データを要求
Process Commander
  ↓
Process Messenger
  ↓
Data Messenger
  ↓
Data Commander
  ↓
Data Processing
```

Data Processing は取得・保存・整形を行い、その結果を Data Messenger へ返します。

```text
Data Processing
  ↓
Data Commander
  ↓
Data Messenger
  ↓
Process Messenger
  ↓
Process Commander
  ↓
Process Processing
```

## 計算結果の返却

Process Processing が計算を完了した後、結果は用途に応じて UI / Data へ送られます。

### UIへの返却

```text
Process Processing
  ↓
Process Commander
  ↓
Process Messenger
  ↓
UI Messenger
  ↓
UI Commander
  ↓
UI Processing
  ↓
表示
```

UI Processing は Process の返却値をUI表現へ変換できます。

例:

```text
Process result:
  hp = 24
  max_hp = 100

UI Processing:
  text = "24 / 100"
  gauge_ratio = 0.24
  warning = true
```

### Dataへの返却

保存すべき結果がある場合は Data Layer へ送ります。

```text
Process Processing
  ↓
Process Commander
  ↓
Process Messenger
  ↓
Data Messenger
  ↓
Data Commander
  ↓
Data Processing
```

## 原則

- Messenger は通信だけを行う。
- Commander は処理の呼び分けだけを行う。
- 値の意味に基づく計算は Process Processing が行う。
- 値をどう見せるかは UI Processing が行う。
- 値をどう保存・復元するかは Data Processing が行う。
