# 要件定義 - AdventureWorks 購買管理システム

## システム価値

### システムコンテキスト

```plantuml
@startuml

title システムコンテキスト図 - AdventureWorks 購買管理システム

left to right direction

actor 購買担当者
actor 在庫管理担当者
actor 購買マネージャー

agent ベンダー
agent 在庫管理システム
agent 会計システム

usecase 購買管理システム
note top of 購買管理システム
  製品の発注プロセスを効率化し、
  在庫不足を防ぐための再発注を支援する
  購買業務の総合管理システム
end note

:購買担当者: -- (購買管理システム)
:在庫管理担当者: -- (購買管理システム)
:購買マネージャー: -- (購買管理システム)
(購買管理システム) -- ベンダー
(購買管理システム) -- 在庫管理システム
(購買管理システム) -- 会計システム

@enduml
```

### 要求モデル

```plantuml
@startuml

title 要求モデル図 - AdventureWorks 購買管理システム

left to right direction

actor 購買担当者
note "在庫不足を防ぎたい" as po_r1
note "効率的に発注したい" as po_r2
note "ベンダー情報を管理したい" as po_r3
note as po_dr1 #Turquoise
  自動的に再発注候補を提示
  適切な発注タイミングの判断支援
  ベンダーごとの一括発注
end note
:購買担当者: -- po_r1
:購買担当者: -- po_r2
:購買担当者: -- po_r3
po_r1 -- po_dr1
po_r2 -- po_dr1
po_r3 -- po_dr1

actor 在庫管理担当者
note "在庫状況を把握したい" as im_r1
note "発注状況を確認したい" as im_r2
note "リードタイムを管理したい" as im_r3
note as im_dr1 #Turquoise
  リアルタイムな在庫情報の提供
  発注から入荷までの追跡
  ベンダー別リードタイム分析
end note
:在庫管理担当者: -- im_r1
:在庫管理担当者: -- im_r2
:在庫管理担当者: -- im_r3
im_r1 -- im_dr1
im_r2 -- im_dr1
im_r3 -- im_dr1

actor 購買マネージャー
note "購買実績を分析したい" as pm_r1
note "コスト削減したい" as pm_r2
note "ベンダー評価したい" as pm_r3
note as pm_dr1 #Turquoise
  購買データの可視化と分析
  最適な発注量の提案
  ベンダーパフォーマンス評価
end note
:購買マネージャー: -- pm_r1
:購買マネージャー: -- pm_r2
:購買マネージャー: -- pm_r3
pm_r1 -- pm_dr1
pm_r2 -- pm_dr1
pm_r3 -- pm_dr1

@enduml
```

## システム外部環境

### ビジネスコンテキスト

```plantuml
@startuml

title ビジネスコンテキスト図 - AdventureWorks 購買管理システム

left to right direction

actor 仕入先企業
actor 配送業者

node AdventureWorks社 {
  rectangle 購買部門 {
    actor 購買担当者
    actor 購買マネージャー
  }

  rectangle 倉庫部門 {
    actor 在庫管理担当者
  }

  usecase 購買業務
  usecase 在庫管理業務
  usecase ベンダー管理業務

  artifact 発注書
  artifact 在庫データ
}

node 外部システム {
  agent ベンダーシステム
  agent 配送管理システム
}

:仕入先企業: -- (購買業務)
:配送業者: -- (購買業務)

(購買業務) -- :購買担当者:
(在庫管理業務) -- :在庫管理担当者:
(ベンダー管理業務) -- :購買マネージャー:

(購買業務) -- 発注書
(在庫管理業務) -- 在庫データ

(購買業務) -- ベンダーシステム
(購買業務) -- 配送管理システム

@enduml
```

### ビジネスユースケース

#### 購買業務

```plantuml
@startuml

title ビジネスユースケース図 - 購買業務

left to right direction

actor 購買担当者
actor 購買マネージャー
actor ベンダー

agent 購買部門

usecase 再発注要求確認 as uc_01
usecase 発注処理 as uc_02
usecase 発注承認 as uc_03

artifact 要再発注リスト as af_01
artifact 発注書 as af_02
artifact 承認済み発注書 as af_03

:購買担当者: -- (uc_01)
:購買担当者: -- (uc_02)
:購買マネージャー: -- (uc_03)
:ベンダー: -- (uc_02)

(uc_01) -- 購買部門
(uc_02) -- 購買部門
(uc_03) -- 購買部門

(uc_01) -- af_01
(uc_02) -- af_02
(uc_03) -- af_03

@enduml
```

#### 在庫管理業務

```plantuml
@startuml

title ビジネスユースケース図 - 在庫管理業務

left to right direction

actor 在庫管理担当者
actor 購買担当者

agent 倉庫部門

usecase 在庫確認 as uc_01
usecase 入荷処理 as uc_02

artifact 在庫台帳 as af_01
artifact 入荷記録 as af_02

:在庫管理担当者: -- (uc_01)
:在庫管理担当者: -- (uc_02)
:購買担当者: -- (uc_01)

(uc_01) -- 倉庫部門
(uc_02) -- 倉庫部門

(uc_01) -- af_01
(uc_02) -- af_02

@enduml
```

### 業務フロー

#### 再発注処理の業務フロー

```plantuml
@startuml

title 業務フロー図 - 再発注処理

|購買担当者|
partition 発注準備 {
  :要再発注製品一覧を確認;
  :ベンダーごとに製品を選択;
  :発注内容を確認;
}

|購買マネージャー|
partition 承認 {
  :発注内容を確認;
  if (承認基準を満たす？) then (yes)
    :発注を承認;
  else (no)
    :修正を依頼;
    stop
  endif
}

|購買担当者|
partition 発注実行 {
  :発注書を作成;
  :ベンダーへ送信;
  :発注記録を保存;
}
stop

@enduml
```

#### 在庫確認の業務フロー

```plantuml
@startuml
title 業務フロー図 - 在庫確認

|在庫管理担当者|
start
:在庫レベルを確認;
if (在庫が閾値以下？) then (yes)
  :再発注要求を作成;
else (no)
  :在庫レベル正常;
endif

|購買担当者|
:再発注要求を受信;
:発注処理を開始;

|在庫管理担当者|
:発注完了通知を受信;
:納期を記録;
stop

@enduml
```

### 利用シーン

#### 再発注処理の利用シーン

```plantuml
@startuml

title 利用シーン図 - 再発注処理

left to right direction

actor 購買担当者
actor 購買マネージャー

frame 定期発注シーン
note right of 定期発注シーン
  週次/月次の定期的な発注処理
  複数ベンダーへの一括発注
  コスト最適化を考慮した発注
  リードタイムを考慮した計画的発注
end note

frame 緊急発注シーン
note right of 緊急発注シーン
  在庫切れリスクの高い製品の緊急発注
  短納期での対応が必要な場合
  優先ベンダーへの即時発注
  承認プロセスの簡略化
end note

usecase 要再発注製品確認
usecase 発注書作成
usecase 発注承認

:購買担当者: -- 定期発注シーン
:購買マネージャー: -- 定期発注シーン
定期発注シーン -- (要再発注製品確認)
定期発注シーン -- (発注書作成)

:購買担当者: -- 緊急発注シーン
緊急発注シーン -- (発注承認)

@enduml
```

### バリエーション・条件

#### ベンダー種別

| 分類 | 説明 |
|------|------|
| 優先ベンダー | 信頼性が高く、優先的に発注を行うベンダー |
| 通常ベンダー | 一般的な取引条件のベンダー |
| スポットベンダー | 特定の製品や緊急時のみ利用するベンダー |

#### 発注種別

| 分類 | 説明 |
|------|------|
| 定期発注 | スケジュールに基づく計画的な発注 |
| 補充発注 | 在庫水準に基づく自動的な発注 |
| 緊急発注 | 在庫切れリスクに対応する即時発注 |

#### 承認レベル

| 分類 | 説明 |
|------|------|
| 自動承認 | 一定金額以下の定期発注 |
| 一次承認 | 購買担当者による承認が必要 |
| 二次承認 | 購買マネージャーによる承認が必要 |

## システム境界

### ユースケース複合図

#### 再発注処理

```plantuml
@startuml

title ユースケース複合図 - 再発注処理

left to right direction

actor 購買担当者 as user
actor 購買マネージャー as manager

frame "定期発注シーン" as f01
usecase "要再発注製品を確認する" as UC1
usecase "発注を作成する" as UC2
boundary "要再発注製品一覧画面" as b01
boundary "発注作成画面" as b02
entity "要再発注製品" as e01
entity "発注" as e02
control "在庫閾値判定" as c01
control "発注金額計算" as c02

user -- f01
f01 -- UC1
f01 -- UC2

b01 -- UC1
UC1 -- e01
UC1 -- c01

b02 -- UC2
UC2 -- e02
UC2 -- c02

frame "承認シーン" as f02
usecase "発注を承認する" as UC3
boundary "発注承認画面" as b03
entity "承認済み発注" as e03
control "承認権限確認" as c03

manager -- f02
f02 -- UC3
b03 -- UC3
UC3 -- e03
UC3 -- c03

@enduml
```

#### 在庫管理

```plantuml
@startuml

title ユースケース複合図 - 在庫管理

left to right direction

actor 在庫管理担当者 as user

frame "在庫確認シーン" as f01
usecase "在庫レベルを確認する" as UC1
usecase "再発注要求を作成する" as UC2
boundary "在庫状況画面" as b01
boundary "再発注要求画面" as b02
entity "在庫情報" as e01
entity "再発注要求" as e02
control "在庫閾値判定" as c01
interface "在庫変動イベント" as i01

user -- f01
f01 -- UC1
f01 -- UC2

b01 -- UC1
UC1 -- e01
UC1 -- c01

b02 -- UC2
UC2 -- e02
UC2 -- i01

@enduml
```

## システム

### 情報モデル

```plantuml
@startuml

title 情報モデル図 - AdventureWorks 購買管理システム

left to right direction

' 購買関連
entity 発注
entity 発注明細
entity ベンダー
entity ベンダー製品

' 製品関連
entity 製品
entity 製品カテゴリ
entity 製品サブカテゴリ

' 在庫・再発注関連
entity 要再発注製品
entity 在庫情報

' 配送関連
entity 配送方法

' ユーザー関連
entity 従業員
entity ユーザー

' 関連付け
発注 "1" -- "*" 発注明細
発注 -- ベンダー
発注 -- 配送方法
発注 -- 従業員

ベンダー "1" -- "*" ベンダー製品
ベンダー製品 -- 製品

製品 -- 製品サブカテゴリ
製品サブカテゴリ -- 製品カテゴリ

要再発注製品 -- 製品
要再発注製品 -- ベンダー

在庫情報 -- 製品

従業員 -- ユーザー

@enduml
```

### 状態モデル

#### 発注の状態遷移

```plantuml
@startuml
[*] --> 保留中

保留中 --> 承認済み : UC:発注承認
保留中 --> キャンセル : UC:発注キャンセル

state 承認済み {
  [*] --> 処理中
  処理中 --> 配送中 : UC:配送開始
  配送中 --> 入荷済み : UC:入荷処理
}

承認済み --> 完了 : UC:発注完了
承認済み --> キャンセル : UC:発注キャンセル

完了 --> [*]
キャンセル --> [*]
@enduml
```

#### 在庫の状態遷移

```plantuml
@startuml
title 在庫の状態遷移図

[*] --> 正常 : UC:初期登録

正常 --> 要発注 : UC:在庫確認
要発注 --> 発注済み : UC:発注作成
発注済み --> 入荷待ち : UC:発注承認
入荷待ち --> 正常 : UC:入荷処理

正常 --> 在庫切れ : UC:在庫確認
在庫切れ --> 発注済み : UC:緊急発注

発注済み --> キャンセル : UC:発注キャンセル
キャンセル --> 要発注
要発注 --> [*]
@enduml
```

---

## 記入ガイドに基づく補足説明

### システムの目的とビジョン

AdventureWorks 購買管理システムは、製品の発注プロセスを効率化し、適切な在庫水準を維持することで、ビジネスの継続性を確保します。特に再発注処理の自動化により、在庫切れリスクの削減と購買業務の効率化を実現します。

### ドメイン用語集

- **要再発注製品 (RequiringPurchaseProduct)**: 在庫レベルが閾値を下回り、発注が必要な製品
- **ベンダー (Vendor)**: 製品の仕入先となる取引先企業
- **リードタイム (LeadTime)**: 発注から納品までに要する日数
- **発注承認 (Purchase Approval)**: 発注金額や条件に基づく上位者による承認プロセス
- **在庫閾値 (Inventory Threshold)**: 再発注が必要となる在庫数量の基準値

### トレーサビリティ

本要件定義書は、実際のソースコードから抽出されたドメインモデルとユースケースに基づいて作成されています。各エンティティとその関係性は、実装されているクラス構造と対応しており、開発フェーズでの詳細設計に直接活用できます。