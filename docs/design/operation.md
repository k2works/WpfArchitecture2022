# 運用要件定義 - AdventureWorks 購買管理システム

## 概要

本ドキュメントでは、AdventureWorks 購買管理システムの運用に関する要件を定義します。Clean Architecture と Domain-Driven Design を採用したクライアント・サーバー型システムの運用特性を考慮した包括的な運用要件を提供します。

## システム運用アーキテクチャ

### システム構成概要

```plantuml
@startuml
skinparam component {
  BackgroundColor<<Client>> #E8F5E8
  BackgroundColor<<Server>> #FFF3E0
  BackgroundColor<<Database>> #FFE0E0
  BackgroundColor<<Monitoring>> #E0E8FF
}

package "クライアント環境" {
  component "WPF Client\n(MVVM + Kamishibai)" <<Client>> as client
}

package "サーバー環境" {
  component "MagicOnion Server\n(Clean Architecture)" <<Server>> as server
  component "Application Services" <<Server>> as app
  component "Domain Services" <<Server>> as domain
  component "Infrastructure Layer" <<Server>> as infra
}

package "データ環境" {
  database "SQL Server\nDatabase" <<Database>> as db
}

package "運用監視環境" {
  component "ログ監視システム" <<Monitoring>> as log
  component "パフォーマンス監視" <<Monitoring>> as perf
  component "アラート通知システム" <<Monitoring>> as alert
}

client -> server : MagicOnion RPC
server -> app
app -> domain
app -> infra
infra -> db

server -> log
server -> perf
perf -> alert

@enduml
```

### 運用環境構成

| 環境種別 | 目的 | 構成 | データ同期 |
|---------|------|-----|----------|
| 開発環境 | 開発・単体テスト | 1台構成 | 開発用テストデータ |
| テスト環境 | 統合テスト・受入テスト | 2台構成 | 本番相当データ（マスキング済み） |
| ステージング環境 | 本番前検証 | 本番同等構成 | 本番データのコピー（マスキング済み） |
| 本番環境 | サービス提供 | 冗長構成 | リアルタイムデータ |

## インフラストラクチャ運用要件

### サーバー要件

#### アプリケーションサーバー

**最小要件**:
- CPU: 4コア以上
- メモリ: 8GB以上
- ストレージ: 100GB以上（SSD推奨）
- OS: Windows Server 2019 以上または Linux（.NET Core対応）

**推奨要件**:
- CPU: 8コア以上
- メモリ: 16GB以上
- ストレージ: 500GB以上（NVMe SSD）
- 冗長化: 最低2台構成（ロードバランサー使用）

#### データベースサーバー

**最小要件**:
- CPU: 8コア以上
- メモリ: 16GB以上
- ストレージ: 500GB以上（SSD RAID1構成）
- SQL Server 2019 Standard Edition 以上

**推奨要件**:
- CPU: 16コア以上
- メモリ: 32GB以上
- ストレージ: 1TB以上（SSD RAID10構成）
- SQL Server 2019 Enterprise Edition
- Always On 可用性グループ構成

### クライアント要件

#### ハードウェア要件

- CPU: Intel Core i5 以上または AMD Ryzen 5 以上
- メモリ: 8GB以上
- ストレージ: 50GB以上の空き容量
- 画面解像度: 1920×1080以上
- ネットワーク: 有線LAN推奨（最低10Mbps）

#### ソフトウェア要件

- OS: Windows 10 Pro 以上（最新アップデート適用）
- .NET Framework 4.8 以上
- Microsoft Visual C++ 2019 Redistributable
- Windows Defender または企業向けアンチウイルスソフト

### ネットワーク要件

#### 通信要件

```plantuml
@startuml
skinparam rectangle {
  BackgroundColor<<Client>> #E8F5E8
  BackgroundColor<<Server>> #FFF3E0
  BackgroundColor<<Database>> #FFE0E0
}

rectangle "クライアントセグメント\n192.168.1.0/24" <<Client>> as client_net
rectangle "アプリケーションセグメント\n192.168.10.0/24" <<Server>> as app_net
rectangle "データベースセグメント\n192.168.20.0/24" <<Database>> as db_net

client_net --> app_net : MagicOnion\nTCP/12345
app_net --> db_net : SQL Server\nTCP/1433

note right of client_net
帯域幅: 最低10Mbps
レイテンシ: 100ms以下
同時接続: 最大100クライアント
end note

note right of app_net
高可用性構成
ロードバランシング
SSL/TLS暗号化
end note

@enduml
```

#### ファイアウォール設定

| 通信方向 | プロトコル | ポート | 用途 |
|---------|-----------|-------|------|
| Client → App | TCP | 12345 | MagicOnion RPC |
| App → DB | TCP | 1433 | SQL Server |
| Monitoring → App | TCP | 80/443 | ヘルスチェック |
| Admin → All | TCP | 3389 | リモートデスクトップ |

## デプロイ・配置運用

### デプロイ戦略

#### ブルーグリーンデプロイ

```plantuml
@startuml
skinparam rectangle {
  BackgroundColor<<Blue>> #E0F2FF
  BackgroundColor<<Green>> #E0FFE0
  BackgroundColor<<LB>> #FFE0E0
}

rectangle "ロードバランサー" <<LB>> as lb

rectangle "Blue環境" <<Blue>> as blue {
  rectangle "App Server 1" as blue1
  rectangle "App Server 2" as blue2
}

rectangle "Green環境" <<Green>> as green {
  rectangle "App Server 3" as green3
  rectangle "App Server 4" as green4
}

lb --> blue : "現在のトラフィック"
lb ..> green : "次回デプロイ先"

note right of green
  1. Green環境にデプロイ
  2. ヘルスチェック実行
  3. トラフィック切り替え
  4. Blue環境を次回デプロイ用に
end note

@enduml
```

#### デプロイフロー

```plantuml
@startuml
start

:ソースコードコミット;
:CI/CDパイプライン実行;
:単体テスト実行;

if (テスト成功?) then (yes)
  :ビルド成果物作成;
  :ステージング環境デプロイ;
  :統合テスト実行;

  if (統合テスト成功?) then (yes)
    :本番環境デプロイ承認待ち;
    :本番環境デプロイ実行;
    :ヘルスチェック;

    if (ヘルスチェック成功?) then (yes)
      :デプロイ完了;
      stop
    else (no)
      :自動ロールバック;
      :デプロイ失敗通知;
      stop
    endif
  else (no)
    :ロールバック;
    :失敗通知;
    stop
  endif
else (no)
  :ビルド失敗通知;
  stop
endif

@enduml
```

### クライアントデプロイ

#### ClickOnce配布

```csharp
// 自動更新機能の実装例
public class ApplicationUpdater
{
    public async Task CheckForUpdatesAsync()
    {
        if (ApplicationDeployment.IsNetworkDeployed)
        {
            var deployment = ApplicationDeployment.CurrentDeployment;
            var updateInfo = await deployment.CheckForDetailedUpdateAsync();

            if (updateInfo.UpdateAvailable)
            {
                // ユーザーに更新通知
                var result = MessageBox.Show(
                    "新しいバージョンが利用可能です。更新しますか？",
                    "アップデート通知",
                    MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    deployment.UpdateAsync();
                }
            }
        }
    }
}
```

## 監視・運用

### システム監視要件

#### パフォーマンス監視

**サーバーサイド監視項目**:
- CPU使用率: 80%未満を維持
- メモリ使用率: 75%未満を維持
- ディスクI/O: IOPS監視とレスポンス時間
- ネットワーク帯域: 使用率と接続数
- MagicOnion RPC応答時間: 平均500ms以下

**データベース監視項目**:
- クエリ実行時間: 長時間実行クエリの検出
- ロック待機: デッドロックの監視
- インデックス効率: クエリプランの監視
- 接続プール: アクティブ接続数の監視

**クライアント監視項目**:
- メモリリーク: ガベージコレクション監視
- UI応答性: フレームレート監視
- ネットワーク接続: 切断・再接続の監視

#### 監視ダッシュボード

```plantuml
@startuml
skinparam rectangle {
  BackgroundColor<<Critical>> #FFE0E0
  BackgroundColor<<Warning>> #FFF3E0
  BackgroundColor<<Normal>> #E8F5E8
}

rectangle "システム監視ダッシュボード" {
  rectangle "サーバー監視" <<Normal>> as server_mon
  rectangle "アプリケーション監視" <<Warning>> as app_mon
  rectangle "データベース監視" <<Normal>> as db_mon
  rectangle "アラート管理" <<Critical>> as alert_mgmt
}

note right of server_mon
CPU使用率
メモリ使用率
ディスクI/O
ネットワーク
end note

note right of app_mon
RPC応答時間
エラー率
同時接続数
スループット
end note

note right of db_mon
クエリ実行時間
接続プール
ロック待機
レプリケーション遅延
end note

note right of alert_mgmt
緊急アラート
警告アラート
通知履歴
エスカレーション
end note

@enduml
```

### ログ管理

#### ログレベル定義

| レベル | 用途 | 出力条件 | 保存期間 |
|--------|------|---------|---------|
| FATAL | システム停止レベルのエラー | 常時 | 永続保存 |
| ERROR | アプリケーションエラー | 常時 | 1年間 |
| WARN | 警告事象 | 常時 | 6ヶ月間 |
| INFO | 業務処理の開始・終了 | 常時 | 3ヶ月間 |
| DEBUG | デバッグ情報 | 開発・テスト環境のみ | 1ヶ月間 |

#### ログ出力実装例

```csharp
// Domain層でのログ出力例
public class PurchaseOrderDomainService
{
    private readonly ILogger<PurchaseOrderDomainService> _logger;

    public async Task<PurchaseOrder> CreatePurchaseOrderAsync(
        IEnumerable<RequiringPurchaseProduct> requiringProducts)
    {
        _logger.LogInformation("発注書作成開始. 対象商品数: {ProductCount}",
            requiringProducts.Count());

        try
        {
            var purchaseOrder = new PurchaseOrder(/* parameters */);

            _logger.LogInformation("発注書作成完了. 発注書ID: {PurchaseOrderId}",
                purchaseOrder.Id.Value);

            return purchaseOrder;
        }
        catch (DomainException ex)
        {
            _logger.LogError(ex, "発注書作成でドメインエラーが発生: {ErrorMessage}",
                ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogFatal(ex, "発注書作成で予期しないエラーが発生");
            throw;
        }
    }
}
```

### アラート運用

#### アラート設定

**緊急アラート（即座対応）**:
- システムダウン
- データベース接続エラー
- メモリ使用率95%以上
- 応答時間5秒以上

**警告アラート（24時間以内対応）**:
- CPU使用率80%以上継続
- エラー率5%以上
- ディスク使用率85%以上
- 同時接続数上限の80%到達

**通知アラート（定期確認）**:
- バッチ処理の完了通知
- バックアップ完了通知
- 定期メンテナンス通知

## バックアップ・リストア運用

### バックアップ戦略

#### データベースバックアップ

```plantuml
@startuml
skinparam rectangle {
  BackgroundColor<<Full>> #FFE0E0
  BackgroundColor<<Diff>> #FFF3E0
  BackgroundColor<<Log>> #E8F5E8
}

rectangle "バックアップスケジュール" {
  rectangle "完全バックアップ" <<Full>> as full
  rectangle "差分バックアップ" <<Diff>> as diff
  rectangle "ログバックアップ" <<Log>> as log
}

note right of full
実行: 毎日 AM 2:00
保存期間: 30日間
保存先: 外部ストレージ
end note

note right of diff
実行: 6時間間隔
保存期間: 7日間
保存先: ローカルストレージ
end note

note right of log
実行: 15分間隔
保存期間: 24時間
保存先: ローカルストレージ
end note

full --> diff : "完全バックアップ後"
diff --> log : "差分バックアップ間"

@enduml
```

#### バックアップ検証

```sql
-- バックアップ整合性チェック用クエリ
RESTORE VERIFYONLY
FROM DISK = 'C:\Backup\AdventureWorksPurchasing_Full.bak'
WITH CHECKSUM;

-- データ整合性チェック
DBCC CHECKDB('AdventureWorksPurchasing')
WITH NO_INFOMSGS, ALL_ERRORMSGS;
```

### 災害復旧（DR）

#### RTO/RPO設定

| 障害レベル | RTO（目標復旧時間） | RPO（目標復旧時点） | 復旧方法 |
|----------|-------------------|-------------------|---------|
| アプリケーション障害 | 30分以内 | 0分 | 自動再起動・切り替え |
| サーバー障害 | 2時間以内 | 15分以内 | 冗長サーバーへの切り替え |
| データベース障害 | 4時間以内 | 1時間以内 | Always Onフェイルオーバー |
| サイト全体障害 | 24時間以内 | 4時間以内 | DRサイトでの復旧 |

#### 復旧手順

```plantuml
@startuml
start

:障害検知;
:影響度評価;

if (障害レベル?) then (レベル1: アプリケーション)
  :アプリケーション再起動;
  :正常性確認;
else if (レベル2: サーバー)
  :冗長サーバーへ切り替え;
  :負荷分散設定更新;
else if (レベル3: データベース)
  :Always Onフェイルオーバー;
  :アプリケーション接続先変更;
else (レベル4: サイト全体)
  :DRサイト活性化;
  :DNS切り替え;
  :全サービス復旧確認;
endif

:ユーザー通知;
:復旧完了報告;
:事後分析実施;

stop
@enduml
```

## セキュリティ運用

### アクセス制御

#### 認証・認可モデル

```plantuml
@startuml
skinparam actor {
  BackgroundColor<<User>> #E8F5E8
  BackgroundColor<<Admin>> #FFE0E0
}

actor "購買担当者" <<User>> as buyer
actor "承認者" <<User>> as approver
actor "システム管理者" <<Admin>> as admin

rectangle "認証・認可システム" {
  rectangle "Active Directory" as ad
  rectangle "ロールベースアクセス制御" as rbac
  rectangle "監査ログ" as audit
}

buyer --> ad : "ドメイン認証"
approver --> ad : "ドメイン認証"
admin --> ad : "管理者認証"

ad --> rbac : "ロール情報"
rbac --> audit : "アクセス記録"

note right of rbac
  【ロール定義】
  - 購買担当者: 商品検索、発注書作成
  - 承認者: 発注承認、履歴確認
  - システム管理者: 全機能、設定変更
end note

@enduml
```

#### セキュリティ監視

```csharp
// セキュリティ監査ログの実装例
public class SecurityAuditService
{
    private readonly ILogger<SecurityAuditService> _logger;

    public void LogUserAccess(string userId, string action, string resource)
    {
        _logger.LogInformation(
            "ユーザーアクセス: UserId={UserId}, Action={Action}, Resource={Resource}, Timestamp={Timestamp}",
            userId, action, resource, DateTime.UtcNow);
    }

    public void LogSecurityEvent(SecurityEventType eventType, string details)
    {
        _logger.LogWarning(
            "セキュリティイベント: EventType={EventType}, Details={Details}, Timestamp={Timestamp}",
            eventType, details, DateTime.UtcNow);
    }
}
```

### 脆弱性管理

#### 定期セキュリティ対応

| 対応項目 | 頻度 | 責任者 | 成果物 |
|---------|------|-------|-------|
| セキュリティパッチ適用 | 月次 | システム管理者 | パッチ適用報告書 |
| 脆弱性スキャン | 四半期 | セキュリティ担当 | 脆弱性評価報告書 |
| ペネトレーションテスト | 年次 | 外部業者 | セキュリティ監査報告書 |
| セキュリティ教育 | 半期 | 全従業員 | 教育受講証明書 |

## 保守・メンテナンス運用

### 定期メンテナンス

#### メンテナンススケジュール

```plantuml
@startuml
gantt
    title システム定期メンテナンススケジュール
    dateFormat  YYYY-MM-DD
    section 月次メンテナンス
    データベース統計更新        :done, monthly1, 2024-01-15, 2h
    インデックス再構築          :done, monthly2, after monthly1, 3h
    ログファイルクリーンアップ   :done, monthly3, after monthly2, 1h

    section 四半期メンテナンス
    システム全体バックアップ    :quarterly1, 2024-03-15, 6h
    パフォーマンス分析         :quarterly2, after quarterly1, 4h
    容量計画レビュー           :quarterly3, after quarterly2, 2h

    section 年次メンテナンス
    システム全体検査          :yearly1, 2024-12-15, 24h
    DR環境動作確認           :yearly2, after yearly1, 8h
@enduml
```

#### メンテナンス実行手順

```powershell
# データベースメンテナンススクリプト例
# インデックス再構築
$SqlInstance = "PROD-DB-01"
$Database = "AdventureWorksPurchasing"

# 統計情報更新
Invoke-Sqlcmd -ServerInstance $SqlInstance -Database $Database -Query @"
UPDATE STATISTICS Product WITH FULLSCAN;
UPDATE STATISTICS Vendor WITH FULLSCAN;
UPDATE STATISTICS PurchaseOrder WITH FULLSCAN;
"@

# インデックス再構築
Invoke-Sqlcmd -ServerInstance $SqlInstance -Database $Database -Query @"
ALTER INDEX ALL ON Product REBUILD WITH (ONLINE = ON);
ALTER INDEX ALL ON Vendor REBUILD WITH (ONLINE = ON);
ALTER INDEX ALL ON PurchaseOrder REBUILD WITH (ONLINE = ON);
"@

# ログファイルクリーンアップ
Get-ChildItem "C:\Logs\AdventureWorksPurchasing\" -Name "*.log" |
Where-Object {$_.LastWriteTime -lt (Get-Date).AddDays(-30)} |
Remove-Item -Force
```

### 容量計画

#### リソース使用量予測

```plantuml
@startuml
skinparam rectangle {
  BackgroundColor<<Current>> #E8F5E8
  BackgroundColor<<Projected>> #FFF3E0
  BackgroundColor<<Alert>> #FFE0E0
}

rectangle "容量計画（6ヶ月予測）" {
  rectangle "データベース容量" <<Current>> as db_capacity
  rectangle "ログ容量" <<Current>> as log_capacity
  rectangle "バックアップ容量" <<Projected>> as backup_capacity
  rectangle "容量アラート" <<Alert>> as capacity_alert
}

note right of db_capacity
現在: 500GB
月間増加: 50GB
予測: 800GB
end note

note right of log_capacity
現在: 100GB
月間増加: 20GB
予測: 220GB
end note

note right of backup_capacity
現在: 1TB
月間増加: 100GB
予測: 1.6TB
end note

note right of capacity_alert
データベース: 80%到達時
ログ: 75%到達時
バックアップ: 85%到達時
end note

@enduml
```

## 運用プロセス・手順

### インシデント管理

#### インシデント分類・優先度

| 優先度 | 影響範囲 | 対応時間 | エスカレーション | 例 |
|--------|---------|---------|----------------|---|
| P1（緊急） | 全システム停止 | 即座 | 30分以内に管理者 | システム全体ダウン |
| P2（高） | 一部機能停止 | 2時間以内 | 4時間以内に管理者 | 発注機能エラー |
| P3（中） | パフォーマンス劣化 | 8時間以内 | 24時間以内に管理者 | 応答時間遅延 |
| P4（低） | 軽微な不具合 | 72時間以内 | 1週間以内に担当者 | 画面表示崩れ |

#### インシデント対応フロー

```plantuml
@startuml
start

:インシデント検知;
:初期トリアージ;

if (優先度?) then (P1: 緊急)
  :即座対応開始;
  :管理者へ即座通知;
  fork
    :一次対応実施;
  fork again
    :根本原因調査;
  end fork
else if (P2: 高)
  :2時間以内に対応開始;
  :関係者へ通知;
else if (P3: 中)
  :8時間以内に対応開始;
  :チケット登録;
else (P4: 低)
  :72時間以内に対応開始;
  :バックログ登録;
endif

:対応実施;
:検証・テスト;

if (解決確認?) then (完了)
  :解決報告;
  :事後分析;
  :ナレッジ更新;
else (未解決)
  :エスカレーション;
  :追加調査;
endif

stop
@enduml
```

### 変更管理

#### 変更要求プロセス

```plantuml
@startuml
start

:変更要求受付;
:影響度分析;

if (変更レベル?) then (緊急)
  :緊急変更委員会承認;
  :即座実装;
else if (標準)
  :変更委員会承認;
  :計画実装;
else (通常)
  :担当者承認;
  :通常実装;
endif

:変更実装;
:テスト実施;
:本番適用;
:事後確認;

:変更完了報告;
stop
@enduml
```

### パフォーマンス管理

#### 性能ベースライン

| 項目 | ベースライン値 | 警告閾値 | 重要閾値 | 測定方法 |
|------|---------------|---------|---------|---------|
| レスポンス時間（商品検索） | 200ms | 500ms | 1000ms | APM監視 |
| レスポンス時間（発注書作成） | 300ms | 800ms | 1500ms | APM監視 |
| スループット | 100 TPS | 80 TPS | 60 TPS | ロードバランサー監視 |
| 同時接続数 | 50ユーザー | 80ユーザー | 100ユーザー | アプリケーション監視 |
| データベース応答時間 | 50ms | 200ms | 500ms | SQL Server監視 |

## SLA・可用性要件

### サービスレベル目標

#### 可用性目標

| サービス | 稼働率目標 | 月間許容停止時間 | 測定方法 |
|---------|-----------|----------------|---------|
| システム全体 | 99.5% | 3.6時間 | 外形監視 |
| MagicOnion API | 99.8% | 1.4時間 | ヘルスチェック |
| データベース | 99.9% | 43分 | 接続監視 |
| クライアント機能 | 99.0% | 7.2時間 | ユーザー報告 |

#### パフォーマンス目標

| 指標 | 目標値 | 測定条件 | 報告頻度 |
|------|-------|---------|---------|
| 平均レスポンス時間 | 500ms以下 | 通常業務時間 | 日次 |
| 95パーセンタイルレスポンス時間 | 1秒以下 | 通常業務時間 | 日次 |
| エラー率 | 1%以下 | 全時間 | リアルタイム |
| スループット | 最低80 TPS | ピーク時間 | 時間次 |

### SLA違反時の対応

#### エスカレーション手順

```plantuml
@startuml
start

:SLA違反検知;

if (違反レベル?) then (重大)
  :即座にマネージャーへエスカレーション;
  :顧客への即座通知;
  :対策本部設置;
else (軽微)
  :担当チームで対応;
  :4時間以内に状況報告;
endif

:原因調査・対応;
:復旧作業;
:サービス正常化確認;
:事後報告書作成;
:再発防止策策定;

stop
@enduml
```

## 運用コスト管理

### コスト要素

#### インフラストラクチャコスト

| 項目 | 月額概算 | 年額概算 | 備考 |
|------|---------|---------|------|
| サーバーハードウェア | ¥300,000 | ¥3,600,000 | 減価償却含む |
| ソフトウェアライセンス | ¥200,000 | ¥2,400,000 | SQL Server, Windows Server |
| ネットワーク・通信費 | ¥50,000 | ¥600,000 | 専用線・インターネット |
| 電力・設備費 | ¥100,000 | ¥1,200,000 | データセンター費用 |
| **合計** | **¥650,000** | **¥7,800,000** |  |

#### 運用要員コスト

| 役割 | 人数 | 月額単価 | 月額合計 | 年額合計 |
|------|------|---------|---------|---------|
| システム管理者 | 2名 | ¥600,000 | ¥1,200,000 | ¥14,400,000 |
| 運用監視担当 | 3名 | ¥450,000 | ¥1,350,000 | ¥16,200,000 |
| ヘルプデスク | 2名 | ¥350,000 | ¥700,000 | ¥8,400,000 |
| **合計** | **7名** |  | **¥3,250,000** | **¥39,000,000** |

### 運用効率化

#### 自動化による効率改善

```plantuml
@startuml
skinparam rectangle {
  BackgroundColor<<Manual>> #FFE0E0
  BackgroundColor<<Auto>> #E8F5E8
  BackgroundColor<<Semi>> #FFF3E0
}

rectangle "運用タスク自動化" {
  rectangle "完全自動化" <<Auto>> as full_auto
  rectangle "半自動化" <<Semi>> as semi_auto
  rectangle "手動作業" <<Manual>> as manual
}

note right of full_auto
デイリーバックアップ
ログローテーション
パフォーマンス監視
アラート通知
end note

note right of semi_auto
デプロイ実行
セキュリティパッチ適用
容量管理
レポート生成
end note

note right of manual
障害対応
変更管理
事後分析
計画策定
end note

note bottom of full_auto
  作業時間削減: 80%
  人的エラー削減: 95%
end note

note bottom of semi_auto
  作業時間削減: 50%
  品質向上: 70%
end note

@enduml
```

## 教育・トレーニング

### 運用要員のスキル要件

#### 必須スキル

**システム管理者**:
- Windows Server管理（中級以上）
- SQL Server管理（中級以上）
- PowerShell スクリプティング
- ネットワーク基礎知識
- セキュリティ基礎知識

**運用監視担当**:
- システム監視ツール操作
- ログ分析スキル
- 基本的なトラブルシューティング
- インシデント管理プロセス理解

#### 継続教育計画

| 対象者 | 教育内容 | 頻度 | 目的 |
|-------|---------|------|------|
| 全運用要員 | セキュリティ基礎 | 半年毎 | セキュリティ意識向上 |
| システム管理者 | 新技術動向 | 四半期毎 | 技術レベル維持 |
| 運用監視担当 | 監視ツール操作 | 年1回 | 運用品質向上 |
| ヘルプデスク | 接客・対応スキル | 年2回 | サービス品質向上 |

### ドキュメント管理

#### 運用ドキュメント体系

```plantuml
@startuml
skinparam folder {
  BackgroundColor<<Process>> #E8F5E8
  BackgroundColor<<Manual>> #FFF3E0
  BackgroundColor<<Knowledge>> #FFE0E0
}

folder "運用ドキュメント" {
  folder "プロセス文書" <<Process>> {
    file "インシデント管理手順"
    file "変更管理手順"
    file "バックアップ手順"
    file "災害復旧手順"
  }

  folder "操作マニュアル" <<Manual>> {
    file "システム監視マニュアル"
    file "デプロイ作業マニュアル"
    file "メンテナンス作業マニュアル"
    file "トラブルシューティングガイド"
  }

  folder "ナレッジベース" <<Knowledge>> {
    file "過去障害事例集"
    file "よくある質問"
    file "設定パラメータ一覧"
    file "チューニングガイド"
  }
}

@enduml
```

## まとめ

本運用要件定義書は、AdventureWorks 購買管理システムの安定運用を実現するための包括的な要件を定義しました。

### 重要なポイント

1. **高可用性の実現**: 99.5%の稼働率目標と適切な冗長化構成
2. **セキュリティの確保**: 多層防御とアクセス制御の徹底
3. **運用効率化**: 自動化推進による人的エラーの削減
4. **コスト最適化**: 適切なリソース配分と効率的な運用体制
5. **継続的改善**: 監視・分析に基づく改善サイクル

これらの要件を満たすことで、ビジネス要求に応える安定したシステム運用を実現し、「変更を楽に安全にできて役に立つソフトウェア」としての価値を継続的に提供します。