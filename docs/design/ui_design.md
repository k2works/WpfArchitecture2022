# UI 設計 - AdventureWorks 購買管理システム

## 概要

本ドキュメントは、AdventureWorks 購買管理システムのユーザーインターフェース（UI）設計について記述します。オブジェクト指向UI設計（OOUX）の原則に基づき、ユーザビリティと保守性を両立したUI構造を定義します。

---

## UI 設計原則

### オブジェクト指向UI設計（OOUX）の採用

```plantuml
@startuml
title OOUX の4つの原則

package "オブジェクトの特定" {
  object RequiringPurchaseProduct
  object PurchaseOrder
  object Vendor
  object Product
}

package "関係の定義" {
  RequiringPurchaseProduct --> Product : "参照"
  RequiringPurchaseProduct --> Vendor : "ベンダー"
  PurchaseOrder --> Vendor : "発注先"
  PurchaseOrder --> Product : "発注製品"
}

package "アクションの定義" {
  object "製品選択" as select
  object "発注作成" as create
  object "数量変更" as update
  object "承認" as approve
}

package "属性の定義" {
  object "製品名・価格・在庫" as attributes
  object "ベンダー名・信用度" as vendor_attr
  object "発注金額・日付" as order_attr
}

@enduml
```

### UI 設計方針

1. **ユーザー中心設計**: 購買担当者のワークフローに最適化
2. **直感的操作**: オブジェクトの関係を視覚的に表現
3. **効率性**: 最小限のクリックで主要タスクを完了
4. **一貫性**: 統一されたデザインパターンとインタラクション
5. **アクセシビリティ**: キーボードナビゲーションとスクリーンリーダー対応

---

## UI オブジェクト分析

### 1. 主要UI オブジェクト

#### RequiringPurchaseProduct（要再発注製品）
**属性**:
- ProductName（製品名）
- VendorName（ベンダー名）
- CurrentStock（現在在庫）
- RecommendedQuantity（推奨発注数量）
- UnitPrice（単価）
- SubTotal（小計）

**アクション**:
- 選択/選択解除
- 詳細表示
- 発注への追加

#### PurchaseOrder（発注）
**属性**:
- VendorInfo（ベンダー情報）
- OrderItems（発注品目）
- SubTotal（小計）
- TaxAmount（税額）
- Freight（送料）
- TotalDue（総額）

**アクション**:
- 品目の数量変更
- 配送方法選択
- 発注実行
- キャンセル

#### Vendor（ベンダー）
**属性**:
- Name（名前）
- AccountNumber（アカウント番号）
- CreditRating（信用度）
- IsPreferredVendor（優先ベンダーフラグ）
- TaxRate（税率）

**アクション**:
- 詳細表示
- 取扱製品確認

### 2. UI オブジェクト関係図

```plantuml
@startuml
title UI オブジェクト関係

object "MainWindow" as main {
  Navigation: Kamishibai
  Theme: Material Design
}

object "MenuPage" as menu {
  MenuItemButton: 再発注
  Navigation: Commands
}

object "RequiringPurchaseProductsPage" as products_page {
  ProductsGrid: GcSpreadGrid
  BackButton: Navigation
  PurchaseButton: Action
}

object "RePurchasingPage" as order_page {
  VendorInfo: Display
  OrderItems: GcSpreadGrid
  TotalCalculation: Binding
  ShipMethodSelector: ComboBox
}

main --> menu : "Initial Page"
menu --> products_page : "NavigateRePurchasingCommand"
products_page --> order_page : "PurchaseCommand"
order_page --> products_page : "GoBackCommand"

@enduml
```

---

## 画面設計

### 1. メインナビゲーション画面

**目的**: システムの主要機能へのアクセスポイント

```plantuml
@startuml
title メイン画面（MenuPage）

rectangle "AdventureWorks Purchasing" as header

package "Menu Area" {
  rectangle "再発注" as purchase_menu {
    rectangle "📦" as icon
    rectangle "再発注" as label
  }
}

header --> purchase_menu : "Central Navigation"

@enduml
```

**UI コンポーネント**:
- `MenuItemButton`: カスタムユーザーコントロール
  - Icon: FontAwesome アイコン（OpencartBrands）
  - Label: "再発注"
  - Command: `NavigateRePurchasingCommand`
  - Style: 立体的なボタン（DropShadowEffect）

### 2. 要再発注製品一覧画面

**目的**: 発注が必要な製品の確認と選択

```plantuml
@startuml
title 要再発注製品一覧画面

package "Header" {
  rectangle "戻る" as back_btn
}

package "Product List" {
  rectangle "GcSpreadGrid" as grid {
    rectangle "製品一覧" as product_list
    rectangle "選択状態" as selection
  }
}

package "Footer" {
  rectangle "選択ベンダー: {VendorName}" as vendor_info
  rectangle "発注" as purchase_btn
}

back_btn --> grid : "Above"
grid --> vendor_info : "Below"
vendor_info --> purchase_btn : "Right"

@enduml
```

**UI コンポーネント**:
- **Header**:
  - 戻るボタン（`GoBackCommand`）
- **Product Grid**:
  - `GcSpreadGrid`: 高機能データグリッド
  - 単一選択モード
  - 外部SGXML定義でカラム設定
- **Footer**:
  - 選択されたベンダー名の表示
  - 発注ボタン（選択時のみ有効）

### 3. 発注作成画面

**目的**: 具体的な発注内容の確認と実行

```plantuml
@startuml
title 発注作成画面（RePurchasingPage）

package "Header" {
  rectangle "キャンセル" as cancel_btn
}

package "Vendor Info" as vendor_section {
  rectangle "アカウント番号: {AccountNumber}" as account
  rectangle "ベンダー名: {Name}" as name
  rectangle "クレジットレーティング: {CreditRating}" as credit
  rectangle "優先ベンダー: {IsPreferredVendor}" as preferred
}

package "Order Details" {
  rectangle "GcSpreadGrid" as order_grid {
    rectangle "発注品目一覧" as items
  }
}

package "Calculation Section" as calc_section {
  rectangle "税率: {TaxRate}" as tax_rate
  rectangle "総計（税込み）: {TotalPrice}" as total
  rectangle "配送方法" as shipping_combo
}

package "Footer" {
  rectangle "発注" as order_btn
}

cancel_btn --> vendor_section
vendor_section --> order_grid
order_grid --> calc_section
calc_section --> order_btn

@enduml
```

**UI コンポーネント**:
- **Vendor Information**:
  - 2列グリッドレイアウト
  - ラベル：コロン：値の構造
  - 読み取り専用表示
- **Order Items Grid**:
  - `GcSpreadGrid`: 発注品目表示
  - 外部SGXML定義
- **Calculation Panel**:
  - 右寄せレイアウト
  - リアルタイム金額計算
  - 配送方法選択（`C1ComboBox`）

---

## UI コンポーネント設計

### 1. カスタムコントロール

#### MenuItemButton

```xml
<UserControl x:Class="MenuItemButton">
    <Button Width="150" Height="200" Background="White"
            Command="{Binding Command}">
        <Button.Effect>
            <DropShadowEffect Color="MidnightBlue" BlurRadius="12"
                             ShadowDepth="5" Direction="330" />
        </Button.Effect>
        <Grid Margin="10">
            <Grid.RowDefinitions>
                <RowDefinition Height="100"/>
                <RowDefinition Height="*"/>
            </Grid.RowDefinitions>
            <iconPacks:PackIconFontAwesome
                Kind="{Binding Icon}"
                Width="50" Height="50"/>
            <TextBlock Grid.Row="1"
                      Text="{Binding MenuLabel}"
                      Style="{StaticResource ContentTextBlock}"/>
        </Grid>
    </Button>
</UserControl>
```

**特徴**:
- 依存関係プロパティ: `Icon`, `MenuLabel`, `Command`
- 立体的な外観（DropShadowEffect）
- FontAwesome アイコン統合
- 統一されたサイズ（150x200）

### 2. データグリッド設定

#### GcSpreadGrid の活用

```xml
<sg:GcSpreadGrid ItemsSource="{Binding Items}"
                 AutoGenerateColumns="False"
                 DocumentUri="/Component/Grid.sgxml"
                 SelectedItem="{Binding SelectedItem, Mode=OneWayToSource}"/>
```

**特徴**:
- 外部XML定義によるカラム設定
- 高度な表示機能（グループ化、フィルタリング）
- Excel風の操作感
- データバインディング対応

### 3. スタイル定義

#### ボタンスタイル

```xml
<!-- PositiveButton: 実行系アクション -->
<Style x:Key="PositiveButton" TargetType="Button">
    <Setter Property="Background" Value="#4CAF50"/>
    <Setter Property="Foreground" Value="White"/>
    <Setter Property="Padding" Value="15,8"/>
    <Setter Property="Margin" Value="5"/>
</Style>

<!-- NegativeButton: キャンセル系アクション -->
<Style x:Key="NegativeButton" TargetType="Button">
    <Setter Property="Background" Value="#F44336"/>
    <Setter Property="Foreground" Value="White"/>
    <Setter Property="Padding" Value="15,8"/>
    <Setter Property="Margin" Value="5"/>
</Style>
```

---

## MVVM パターン実装

### 1. ViewModel 設計

#### RequiringPurchaseProductsViewModel

```plantuml
@startuml
class RequiringPurchaseProductsViewModel {
  + ObservableCollection<RequiringPurchaseProduct> RequiringPurchaseProducts
  + RequiringPurchaseProduct? SelectedRequiringPurchaseProduct
  + IRelayCommand GoBackCommand
  + IAsyncRelayCommand PurchaseCommand
  - IRequiringPurchaseProductQuery _query
  - IVendorRepository _vendorRepository
  - IPresentationService _presentationService
  + Task OnNavigatedAsync(PostForwardEventArgs)
  + Task OnResumingAsync(PreBackwardEventArgs)
  - bool CanPurchase()
}

interface INavigatedAsyncAware
interface IResumingAsyncAware

RequiringPurchaseProductsViewModel ..|> INavigatedAsyncAware
RequiringPurchaseProductsViewModel ..|> IResumingAsyncAware

@enduml
```

**特徴**:
- CommunityToolkit.Mvvm の活用
- Kamishibai による画面ライフサイクル管理
- 依存性注入によるサービス利用
- 自動コマンド生成（`[RelayCommand]`）

#### RePurchasingViewModel

```plantuml
@startuml
class RePurchasingViewModel {
  + Vendor Vendor
  + ObservableCollection<RequiringPurchaseProduct> RequiringPurchaseProducts
  + ObservableCollection<ShipMethod> ShipMethods
  + ShipMethod? SelectedShipMethod
  + Dollar TotalPrice
  + IRelayCommand GoBackCommand
  + IAsyncRelayCommand PurchaseCommand
  - IPurchaseOrderService _purchaseOrderService
  - IPresentationService _presentationService
  + Task OnNavigatedAsync(PostForwardEventArgs, NavigationParameter)
  - void CalculateTotalPrice()
}

class NavigationParameter {
  + Vendor Vendor
  + IEnumerable<RequiringPurchaseProduct> Products
}

RePurchasingViewModel --> NavigationParameter : "Uses"

@enduml
```

### 2. データバインディング戦略

#### 双方向バインディング

```xml
<!-- 選択状態の管理 -->
<DataGrid SelectedItem="{Binding SelectedItem, Mode=OneWayToSource}">

<!-- コンボボックスの選択 -->
<ComboBox SelectedItem="{Binding SelectedShipMethod, Mode=TwoWay}">

<!-- 読み取り専用チェックボックス -->
<CheckBox IsChecked="{Binding IsPreferred, Mode=OneTime}"
          Style="{StaticResource ReadOnlyCheckBox}">
```

#### コンバーター活用

```xml
<UserControl.Resources>
    <converter:DollarConverter x:Key="DollarConverter"/>
</UserControl.Resources>

<TextBlock Text="{Binding TotalPrice, Converter={StaticResource DollarConverter}}"/>
```

---

## ユーザビリティ設計

### 1. ナビゲーションフロー

```plantuml
@startuml
title ユーザーナビゲーションフロー

start
:アプリケーション起動;
:メイン画面表示;
:「再発注」選択;
:要再発注製品一覧表示;

if (製品を選択?) then (yes)
  :選択した製品のベンダー表示;
  :「発注」ボタン有効化;
  if (発注ボタンクリック?) then (yes)
    :発注作成画面表示;
    :ベンダー情報確認;
    :発注品目確認;
    :配送方法選択;
    if (発注実行?) then (yes)
      :発注処理;
      :完了メッセージ;
      :製品一覧に戻る;
    else (no)
      :キャンセル;
    endif
  else (no)
    :選択状態維持;
  endif
else (no)
  :「発注」ボタン無効;
endif

if (戻るボタン?) then (yes)
  :前画面に戻る;
else (no)
  :現在画面維持;
endif

stop

@enduml
```

### 2. エラーハンドリングUI

```plantuml
@startuml
title エラーハンドリング戦略

package "ユーザーアクション" {
  [データ読み込み] as load
  [発注実行] as order
  [画面遷移] as navigate
}

package "エラー種別" {
  [ネットワークエラー] as network
  [データ不整合] as data
  [認証エラー] as auth
  [システムエラー] as system
}

package "ユーザーフィードバック" {
  [MessageBox] as msgbox
  [StatusBar通知] as status
  [インライン警告] as inline
  [リトライオプション] as retry
}

load --> network
load --> data
order --> auth
order --> system
navigate --> network

network --> msgbox
data --> inline
auth --> msgbox
system --> status

msgbox --> retry
inline --> retry

@enduml
```

### 3. アクセシビリティ対応

#### キーボードナビゲーション

```xml
<!-- TabIndex による順序制御 -->
<Button Content="戻る" TabIndex="1"/>
<DataGrid ItemsSource="{Binding Products}" TabIndex="2"/>
<Button Content="発注" TabIndex="3"/>

<!-- AutomationProperties による支援技術対応 -->
<Button Content="発注作成"
        AutomationProperties.Name="発注作成ボタン"
        AutomationProperties.HelpText="選択した製品の発注を作成します"
        ToolTip="選択した製品の発注を作成"/>
```

#### カラーアクセシビリティ

```xml
<!-- コントラスト比を考慮したスタイル -->
<Style x:Key="HighContrastButton" TargetType="Button">
    <Setter Property="Background" Value="#0078D4"/>
    <Setter Property="Foreground" Value="White"/>
    <!-- WCAG AA準拠のコントラスト比 4.5:1以上 -->
</Style>
```

---

## レスポンシブデザイン

### 1. 画面サイズ対応

```plantuml
@startuml
title レスポンシブデザイン戦略

package "画面サイズ" {
  [最小: 1024x768] as min
  [標準: 1366x768] as std
  [大画面: 1920x1080+] as large
}

package "レイアウト調整" {
  [Grid Splitter] as splitter
  [Auto Size Columns] as auto_col
  [Scalable Controls] as scale
}

package "UI要素" {
  [DataGrid] as grid
  [Button Panel] as buttons
  [Information Display] as info
}

min --> splitter
std --> auto_col
large --> scale

splitter --> grid
auto_col --> buttons
scale --> info

@enduml
```

### 2. レイアウト戦略

```xml
<!-- Grid を活用した柔軟レイアウト -->
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>    <!-- Header -->
        <RowDefinition Height="*"/>       <!-- Content -->
        <RowDefinition Height="Auto"/>    <!-- Footer -->
    </Grid.RowDefinitions>

    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto"/>  <!-- Fixed width -->
        <ColumnDefinition Width="*"/>     <!-- Flexible -->
        <ColumnDefinition Width="200"/>   <!-- Min width -->
    </Grid.ColumnDefinitions>
</Grid>
```

---

## パフォーマンス最適化

### 1. データ仮想化

```xml
<!-- 大量データ対応 -->
<DataGrid VirtualizingPanel.IsVirtualizing="True"
          VirtualizingPanel.VirtualizationMode="Recycling"
          ItemsSource="{Binding LargeDataSet}"/>

<!-- GcSpreadGrid による高速描画 -->
<sg:GcSpreadGrid EnableVirtualization="True"
                 LazyLoading="True"
                 ItemsSource="{Binding Products}"/>
```

### 2. 非同期UI更新

```csharp
// ViewModel での非同期データ読み込み
[RelayCommand]
private async Task LoadProductsAsync()
{
    try
    {
        IsLoading = true;

        // バックグラウンドでデータ取得
        var products = await _query.GetRequiringPurchaseProductsAsync();

        // UIスレッドで更新
        Application.Current.Dispatcher.Invoke(() =>
        {
            RequiringPurchaseProducts.Replace(products);
        });
    }
    finally
    {
        IsLoading = false;
    }
}
```

---

## 今後の拡張計画

### 1. 追加画面

```plantuml
@startuml
title 将来的な画面拡張

package "Phase 1 (実装済み)" {
  [Menu Page] as menu
  [Products Page] as products
  [Order Page] as order
}

package "Phase 2 (計画中)" {
  [Order History] as history
  [Vendor Management] as vendor
  [Product Management] as product_mgmt
  [Dashboard] as dashboard
}

package "Phase 3 (将来)" {
  [Approval Workflow] as approval
  [Reporting] as report
  [Analytics] as analytics
}

menu --> products
products --> order
order --> history
menu --> vendor
menu --> product_mgmt
menu --> dashboard
dashboard --> approval
dashboard --> report
report --> analytics

@enduml
```

### 2. 機能強化

- **検索・フィルタリング**: 高度な検索条件による製品絞り込み
- **一括操作**: 複数製品の一括発注
- **承認ワークフロー**: 発注承認プロセスの実装
- **通知機能**: システム通知とアラート
- **テーマ切り替え**: ライト・ダークテーマ対応
- **多言語対応**: 国際化（i18n）機能

---

## まとめ

本UI設計では、以下の主要な設計決定を行いました：

1. **オブジェクト指向UI設計の採用**: ユーザーが操作するオブジェクト中心の直感的設計
2. **MVVM パターンの徹底**: ViewとViewModelの明確な分離
3. **カスタムコントロールの活用**: 再利用可能なUIコンポーネント
4. **データグリッドの最適化**: GcSpreadGridによる高機能表示
5. **アクセシビリティ対応**: 支援技術との互換性確保
6. **レスポンシブデザイン**: 様々な画面サイズへの対応
7. **パフォーマンス最適化**: 仮想化と非同期処理による高速化

この設計により、使いやすく、保守性が高く、拡張可能な購買管理システムのUIを実現します。