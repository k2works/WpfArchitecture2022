# フロントエンドアーキテクチャ設計 - AdventureWorks 購買管理システム

## アーキテクチャ概要

### フロントエンド技術スタック

```plantuml
@startuml
title フロントエンド技術スタック

package "プレゼンテーション層" {
  [WPF Views] as view
  [XAML Templates] as xaml
  [User Controls] as controls
}

package "ビューモデル層" {
  [ViewModels] as vm
  [Commands] as commands
  [Converters] as converters
}

package "サービス層" {
  [MagicOnion Client] as client
  [gRPC Services] as grpc
  [Data Transfer Objects] as dto
}

view --> vm : Data Binding
vm --> client : RPC Calls
client --> grpc : Network
controls --> vm : Commands
xaml --> view : UI Definition

@enduml
```

### アーキテクチャパターン

**Model-View-ViewModel (MVVM) パターン**

```plantuml
@startuml
title MVVM アーキテクチャパターン

package "View 層" {
  [MainWindow] as main
  [PurchaseOrderView] as poView
  [RequiringPurchaseView] as rpView
  [ProductListView] as plView
}

package "ViewModel 層" {
  [MainWindowViewModel] as mainVm
  [PurchaseOrderViewModel] as poVm
  [RequiringPurchaseProductsViewModel] as rpVm
  [ProductListViewModel] as plVm
}

package "Model 層" {
  [Domain Models] as models
  [Service Proxies] as proxies
  [DTOs] as dto
}

main --> mainVm : DataContext
poView --> poVm : DataContext
rpView --> rpVm : DataContext
plView --> plVm : DataContext

mainVm --> models : Business Logic
poVm --> models : Business Logic
rpVm --> models : Business Logic
plVm --> models : Business Logic

models --> proxies : Service Calls
proxies --> dto : Data Transfer

@enduml
```

## View 層設計

### メイン画面構成

```plantuml
@startuml
title メイン画面構成

package "MainWindow" {
  [Navigation Menu] as menu
  [Content Area] as content
  [Status Bar] as status
}

package "Content Views" {
  [RequiringPurchaseView] as rpv
  [PurchaseOrderView] as pov
  [ProductListView] as plv
  [VendorView] as vv
}

menu --> rpv : 要再発注製品
menu --> pov : 発注管理
menu --> plv : 製品一覧
menu --> vv : ベンダー管理

content --> rpv : Display
content --> pov : Display
content --> plv : Display
content --> vv : Display

@enduml
```

### View コンポーネント設計

#### 要再発注製品一覧画面

```plantuml
@startuml
title 要再発注製品一覧画面

package "RequiringPurchaseView" {
  [Search Panel] as search
  [Product Grid] as grid
  [Action Buttons] as buttons
  [Summary Panel] as summary
}

package "子コンポーネント" {
  [Product Row Template] as row
  [Vendor Group Header] as header
  [Filter Controls] as filter
}

search --> filter : フィルター
grid --> row : データテンプレート
grid --> header : グループヘッダー
buttons --> summary : 選択状態連動

@enduml
```

#### 発注作成画面

```plantuml
@startuml
title 発注作成画面

package "PurchaseOrderView" {
  [Order Header] as header
  [Order Details] as details
  [Calculation Panel] as calc
  [Submit Panel] as submit
}

package "詳細コンポーネント" {
  [Product Selection] as selection
  [Quantity Editor] as quantity
  [Price Calculator] as price
  [Delivery Options] as delivery
}

header --> selection : 製品選択
details --> quantity : 数量編集
calc --> price : 金額計算
submit --> delivery : 配送オプション

@enduml
```

## ViewModel 層設計

### CommunityToolkit.Mvvm 活用

```plantuml
@startuml
title ViewModel 基底構造

abstract class BaseViewModel {
  + ObservableObject
  + INotifyPropertyChanged
  + INotifyPropertyChanging
}

class RequiringPurchaseProductsViewModel {
  + ObservableCollection<RequiringPurchaseProduct> Products
  + IAsyncRelayCommand LoadProductsCommand
  + IRelayCommand<RequiringPurchaseProduct> SelectProductCommand
  + IAsyncRelayCommand CreateOrderCommand
  + string SearchText
  + bool IsLoading
}

class PurchaseOrderViewModel {
  + PurchaseOrder Order
  + ObservableCollection<PurchaseOrderDetail> Details
  + IAsyncRelayCommand SaveCommand
  + IRelayCommand AddDetailCommand
  + IAsyncRelayCommand CalculateTotalCommand
  + Dollar SubTotal
  + Dollar TaxAmount
  + Dollar TotalDue
}

BaseViewModel <|-- RequiringPurchaseProductsViewModel
BaseViewModel <|-- PurchaseOrderViewModel

@enduml
```

### Command パターン実装

```csharp
// 例: RequiringPurchaseProductsViewModel のコマンド実装
[RelayCommand]
private async Task LoadProducts()
{
    try
    {
        IsLoading = true;
        var products = await _purchasingService.GetRequiringPurchaseProductsAsync();
        Products.Clear();
        foreach (var product in products)
        {
            Products.Add(product);
        }
    }
    catch (Exception ex)
    {
        // エラーハンドリング
        await ShowErrorMessageAsync(ex.Message);
    }
    finally
    {
        IsLoading = false;
    }
}

[RelayCommand]
private void SelectProduct(RequiringPurchaseProduct product)
{
    if (product != null)
    {
        product.IsSelected = !product.IsSelected;
        UpdateTotalSelection();
    }
}

[RelayCommand]
private async Task CreateOrder()
{
    var selectedProducts = Products.Where(p => p.IsSelected).ToList();
    if (selectedProducts.Any())
    {
        await NavigateToOrderCreation(selectedProducts);
    }
}
```

### データバインディング設計

```xml
<!-- RequiringPurchaseView.xaml の例 -->
<UserControl>
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- 検索パネル -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="10">
            <TextBox Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}"
                     Width="200" Margin="5"/>
            <Button Content="検索" Command="{Binding LoadProductsCommand}" Margin="5"/>
        </StackPanel>

        <!-- 製品グリッド -->
        <DataGrid Grid.Row="1" ItemsSource="{Binding Products}"
                  AutoGenerateColumns="False" Margin="10">
            <DataGrid.Columns>
                <DataGridCheckBoxColumn Binding="{Binding IsSelected}" Header="選択"/>
                <DataGridTextColumn Binding="{Binding Name}" Header="製品名"/>
                <DataGridTextColumn Binding="{Binding VendorName}" Header="ベンダー"/>
                <DataGridTextColumn Binding="{Binding CurrentStock}" Header="現在在庫"/>
                <DataGridTextColumn Binding="{Binding RecommendedQuantity}" Header="推奨発注数"/>
                <DataGridTextColumn Binding="{Binding UnitPrice}" Header="単価"/>
            </DataGrid.Columns>
        </DataGrid>

        <!-- アクションパネル -->
        <StackPanel Grid.Row="2" Orientation="Horizontal"
                    HorizontalAlignment="Right" Margin="10">
            <TextBlock Text="{Binding SelectedCount, StringFormat='選択数: {0}'}"
                       Margin="10,0"/>
            <TextBlock Text="{Binding TotalAmount, StringFormat='合計金額: {0:C}'}"
                       Margin="10,0"/>
            <Button Content="発注する" Command="{Binding CreateOrderCommand}"
                    Margin="5" Padding="10,5"/>
        </StackPanel>
    </Grid>
</UserControl>
```

## サービス層設計

### MagicOnion クライアント実装

```plantuml
@startuml
title MagicOnion クライアント構造

interface IPurchasingService {
  + Task<RequiringPurchaseProduct[]> GetRequiringPurchaseProductsAsync()
  + Task<PurchaseOrder> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest)
  + Task<PurchaseOrder> GetPurchaseOrderAsync(PurchaseOrderId)
  + Task ApproveOrderAsync(PurchaseOrderId)
}

class PurchasingServiceClient {
  + MagicOnionClient<IPurchasingService> client
  + async Task<RequiringPurchaseProduct[]> GetRequiringPurchaseProductsAsync()
  + async Task<PurchaseOrder> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest)
}

interface IProductService {
  + Task<Product[]> GetProductsAsync()
  + Task<Product> GetProductAsync(ProductId)
}

class ProductServiceClient {
  + MagicOnionClient<IProductService> client
  + async Task<Product[]> GetProductsAsync()
}

IPurchasingService <|.. PurchasingServiceClient
IProductService <|.. ProductServiceClient

@enduml
```

### サービスプロキシ実装例

```csharp
public class PurchasingServiceClient : IPurchasingService
{
    private readonly MagicOnionClient<IPurchasingService> _client;

    public PurchasingServiceClient(Channel channel)
    {
        _client = MagicOnionClient.Create<IPurchasingService>(channel);
    }

    public async Task<RequiringPurchaseProduct[]> GetRequiringPurchaseProductsAsync()
    {
        return await _client.GetRequiringPurchaseProductsAsync();
    }

    public async Task<PurchaseOrder> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request)
    {
        return await _client.CreatePurchaseOrderAsync(request);
    }

    public async Task<PurchaseOrder> GetPurchaseOrderAsync(PurchaseOrderId id)
    {
        return await _client.GetPurchaseOrderAsync(id);
    }

    public async Task ApproveOrderAsync(PurchaseOrderId id)
    {
        await _client.ApproveOrderAsync(id);
    }
}
```

## UI/UX 設計原則

### レスポンシブ設計

```plantuml
@startuml
title レスポンシブデザインの考慮

package "画面サイズ対応" {
  [最小サイズ: 1024x768] as min
  [推奨サイズ: 1366x768] as recommended
  [大画面対応: 1920x1080+] as large
}

package "レイアウト戦略" {
  [Grid Layout] as grid
  [Flexible Panels] as panels
  [Scalable Controls] as controls
}

min --> grid : 固定レイアウト
recommended --> panels : 可変レイアウト
large --> controls : スケール対応

@enduml
```

### アクセシビリティ対応

```xml
<!-- アクセシビリティ考慮の例 -->
<Button Content="発注作成"
        Command="{Binding CreateOrderCommand}"
        AutomationProperties.Name="発注作成ボタン"
        AutomationProperties.HelpText="選択した製品の発注を作成します"
        ToolTip="選択した製品の発注を作成"
        TabIndex="1"/>

<DataGrid ItemsSource="{Binding Products}"
          AutomationProperties.Name="要再発注製品一覧"
          AutomationProperties.HelpText="発注が必要な製品の一覧を表示"
          TabIndex="2">
```

### エラーハンドリング UI

```plantuml
@startuml
title エラーハンドリング フロー

start
:ユーザーアクション;
:ViewModel Command実行;

if (例外発生?) then (yes)
  :例外をキャッチ;
  :エラーメッセージ生成;
  :MessageBox/通知表示;
  :ログ出力;
  :UI状態を復元;
else (no)
  :正常処理継続;
endif

stop

@enduml
```

```csharp
// エラーハンドリング実装例
public class BaseViewModel : ObservableObject
{
    protected async Task ExecuteWithErrorHandling(Func<Task> operation,
        string errorTitle = "エラー")
    {
        try
        {
            await operation();
        }
        catch (ServiceException ex)
        {
            await ShowErrorMessage(errorTitle, ex.Message);
            Logger.LogError(ex, "Service error in {ViewModel}", GetType().Name);
        }
        catch (Exception ex)
        {
            await ShowErrorMessage(errorTitle, "予期しないエラーが発生しました。");
            Logger.LogError(ex, "Unexpected error in {ViewModel}", GetType().Name);
        }
    }

    private async Task ShowErrorMessage(string title, string message)
    {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        });
    }
}
```

## パフォーマンス最適化

### データ仮想化

```plantuml
@startuml
title データ仮想化戦略

package "大容量データ対応" {
  [仮想化DataGrid] as virtualGrid
  [ページング] as paging
  [遅延読み込み] as lazy
}

package "メモリ最適化" {
  [WeakReference] as weak
  [IDisposable実装] as disposable
  [リソース解放] as cleanup
}

virtualGrid --> paging : 分割表示
paging --> lazy : 必要時読み込み
weak --> disposable : メモリ管理
disposable --> cleanup : 自動解放

@enduml
```

### 非同期処理パターン

```csharp
// 非同期パターンの実装例
public partial class RequiringPurchaseProductsViewModel : BaseViewModel
{
    private CancellationTokenSource _loadingCancellation;

    [RelayCommand]
    private async Task LoadProducts()
    {
        // 既存の読み込みをキャンセル
        _loadingCancellation?.Cancel();
        _loadingCancellation = new CancellationTokenSource();

        try
        {
            IsLoading = true;

            // プログレス表示
            var progress = new Progress<int>(percentage => LoadingProgress = percentage);

            // 非同期でデータ取得
            var products = await _purchasingService
                .GetRequiringPurchaseProductsAsync(_loadingCancellation.Token, progress);

            // UIスレッドで更新
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Products.Clear();
                foreach (var product in products)
                {
                    Products.Add(product);
                }
            });
        }
        catch (OperationCanceledException)
        {
            // キャンセル時は無視
        }
        catch (Exception ex)
        {
            await ShowErrorMessage("データ読み込みエラー", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

## セキュリティ考慮事項

### 認証・認可

```plantuml
@startuml
title 認証・認可フロー

start
:アプリケーション起動;
:認証状態確認;

if (認証済み?) then (no)
  :ログイン画面表示;
  :認証情報入力;
  :認証サーバー確認;
  if (認証成功?) then (no)
    :エラーメッセージ;
    stop
  else (yes)
    :トークン取得;
  endif
else (yes)
  :既存トークン確認;
endif

:メイン画面表示;
:機能別認可確認;

if (権限あり?) then (yes)
  :機能実行;
else (no)
  :アクセス拒否;
endif

stop

@enduml
```

### データ保護

```csharp
// セキュアなデータハンドリング例
public class SecureDataHandler
{
    // 機密データの暗号化
    public string EncryptSensitiveData(string data)
    {
        // 暗号化ロジック
        return Convert.ToBase64String(
            ProtectedData.Protect(Encoding.UTF8.GetBytes(data), null, DataProtectionScope.CurrentUser));
    }

    // メモリからの機密データクリア
    public void ClearSensitiveData(SecureString secureString)
    {
        secureString?.Clear();
        secureString?.Dispose();
    }

    // 入力値検証
    public bool ValidateInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;

        // SQLインジェクション対策
        var sqlInjectionPattern = @"['"";\-\-]";
        if (Regex.IsMatch(input, sqlInjectionPattern)) return false;

        return true;
    }
}
```

## テスト戦略

### ViewModel 単体テスト

```csharp
// ViewModel テストの例
[TestFixture]
public class RequiringPurchaseProductsViewModelTests
{
    private RequiringPurchaseProductsViewModel _viewModel;
    private Mock<IPurchasingService> _mockService;

    [SetUp]
    public void SetUp()
    {
        _mockService = new Mock<IPurchasingService>();
        _viewModel = new RequiringPurchaseProductsViewModel(_mockService.Object);
    }

    [Test]
    public async Task LoadProducts_Should_PopulateProductsList()
    {
        // Arrange
        var expectedProducts = new[]
        {
            new RequiringPurchaseProduct(/* パラメータ */),
            new RequiringPurchaseProduct(/* パラメータ */)
        };
        _mockService.Setup(s => s.GetRequiringPurchaseProductsAsync())
            .ReturnsAsync(expectedProducts);

        // Act
        await _viewModel.LoadProductsCommand.ExecuteAsync(null);

        // Assert
        Assert.That(_viewModel.Products.Count, Is.EqualTo(2));
        Assert.That(_viewModel.IsLoading, Is.False);
    }

    [Test]
    public void SelectProduct_Should_ToggleSelection()
    {
        // Arrange
        var product = new RequiringPurchaseProduct(/* パラメータ */);
        _viewModel.Products.Add(product);

        // Act
        _viewModel.SelectProductCommand.Execute(product);

        // Assert
        Assert.That(product.IsSelected, Is.True);
    }
}
```

### UI テスト戦略

```plantuml
@startuml
title UI テスト階層

package "単体テスト" {
  [ViewModel Tests] as vm_test
  [Converter Tests] as conv_test
  [Command Tests] as cmd_test
}

package "統合テスト" {
  [Service Integration] as service_test
  [Data Binding Tests] as binding_test
}

package "UIテスト" {
  [画面遷移テスト] as nav_test
  [ユーザー操作テスト] as ui_test
  [アクセシビリティテスト] as a11y_test
}

vm_test --> service_test : Mock から実サービス
conv_test --> binding_test : 単体から統合
cmd_test --> nav_test : コマンドから画面
service_test --> ui_test : バックエンド連携
binding_test --> ui_test : データ表示
nav_test --> a11y_test : 操作性確認

@enduml
```

## デプロイメント考慮事項

### アプリケーション配布

```plantuml
@startuml
title デプロイメント構成

package "開発環境" {
  [Visual Studio] as vs
  [Source Code] as src
  [Unit Tests] as tests
}

package "ビルドパイプライン" {
  [MSBuild] as build
  [NuGet Restore] as nuget
  [Test Runner] as test_run
  [Package Creation] as package
}

package "配布" {
  [ClickOnce] as clickonce
  [MSI Installer] as msi
  [MSIX Package] as msix
}

vs --> src
src --> build
tests --> test_run
build --> nuget
nuget --> package
test_run --> package
package --> clickonce
package --> msi
package --> msix

@enduml
```

### 設定管理

```json
// appsettings.json の例
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "MagicOnion": {
    "ServerAddress": "https://localhost:5001",
    "Timeout": "00:00:30",
    "MaxRetryAttempts": 3
  },
  "UI": {
    "Theme": "Light",
    "Language": "ja-JP",
    "PageSize": 50
  },
  "Features": {
    "EnableAutoSave": true,
    "EnableOfflineMode": false,
    "EnableAdvancedFiltering": true
  }
}
```

## まとめ

本フロントエンドアーキテクチャ設計では、以下の主要な設計決定を行いました：

1. **MVVM パターンの採用**: データバインディングとコマンドパターンによる疎結合設計
2. **CommunityToolkit.Mvvm の活用**: 最新のMVVMライブラリによる生産性向上
3. **MagicOnion クライアント**: 型安全なRPC通信によるバックエンド連携
4. **レスポンシブUI**: 様々な画面サイズに対応する柔軟なレイアウト
5. **非同期パターン**: ユーザー体験を損なわない非ブロッキング処理
6. **エラーハンドリング**: 堅牢なエラー処理とユーザーフレンドリーなメッセージ
7. **テスト戦略**: 単体テストから統合テストまでの包括的テストアプローチ

この設計により、保守性が高く、拡張可能で、ユーザビリティに優れた購買管理システムのフロントエンドを実現します。