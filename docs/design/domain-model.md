# ドメインモデル設計

## 概要

本ドキュメントは、AdventureWorks 購買管理システムのドメインモデル設計を記述します。
ボトムアップアプローチを採用し、値オブジェクトから始めて段階的にエンティティ、集約、ドメインサービスへと構築しています。

## ドメインモデル全体構造

```plantuml
@startuml
skinparam class {
  BackgroundColor<<ValueObject>> #E3F2FD
  BackgroundColor<<Entity>> #E8F5E8
  BackgroundColor<<Service>> #FFF3E0
  BackgroundColor<<Aggregate>> #FFE0E0
}

package "PurchasingDomain" {
  package "ValueObjects" {
    class Dollar <<ValueObject>>
    class Quantity <<ValueObject>>
    class Date <<ValueObject>>
    class Days <<ValueObject>>
    class Gram <<ValueObject>>
    class TaxRate <<ValueObject>>
    class CreditRating <<ValueObject>>
    class AccountNumber <<ValueObject>>
    class ModifiedDateTime <<ValueObject>>
    class RevisionNumber <<ValueObject>>
    class OrderStatus <<ValueObject>>
    class UnitMeasureCode <<ValueObject>>
    class ProductId <<ValueObject>>
    class VendorId <<ValueObject>>
    class PurchaseOrderId <<ValueObject>>
    class PurchaseOrderDetailId <<ValueObject>>
    class EmployeeId <<ValueObject>>
    class ShipMethodId <<ValueObject>>
    class ProductCategoryId <<ValueObject>>
    class ProductSubcategoryId <<ValueObject>>
  }

  package "Entities" {
    class Product <<Entity>>
    class Vendor <<Entity>>
    class ShipMethod <<Entity>>
    class VendorProduct <<Entity>>
    class PurchaseOrderDetail <<Entity>>
  }

  package "Aggregates" {
    class PurchaseOrder <<Aggregate>>
  }

  package "DomainServices" {
    class PurchaseOrderBuilder <<Service>>
  }

  package "RePurchasing" {
    class RequiringPurchaseProduct
  }
}

Product --> ProductId
Product --> Dollar
Product --> Gram

Vendor --> VendorId
Vendor --> AccountNumber
Vendor --> CreditRating
Vendor --> TaxRate
Vendor --> VendorProduct

PurchaseOrder --> PurchaseOrderId
PurchaseOrder --> VendorId
PurchaseOrder --> ShipMethodId
PurchaseOrder --> EmployeeId
PurchaseOrder --> PurchaseOrderDetail

PurchaseOrderBuilder --> Vendor
PurchaseOrderBuilder --> ShipMethod
PurchaseOrderBuilder --> PurchaseOrder

RequiringPurchaseProduct --> VendorId
RequiringPurchaseProduct --> ProductId
RequiringPurchaseProduct --> Dollar
RequiringPurchaseProduct --> Quantity
RequiringPurchaseProduct --> Days

@enduml
```

## 値オブジェクト（Value Objects）

### 金額系値オブジェクト

#### Dollar

```csharp
/// <summary>
/// Dollar
/// </summary>
[UnitOf(typeof(decimal))]
public partial struct Dollar
{
    /// <summary>
    /// 加算
    /// </summary>
    public static Dollar operator +(Dollar z, Dollar w)
    {
        return new Dollar(z.value + w.value);
    }

    /// <summary>
    /// 減算
    /// </summary>
    public static Dollar operator -(Dollar z, Dollar w)
    {
        return new Dollar(z.value - w.value);
    }

    /// <summary>
    /// 乗算
    /// </summary>
    public static Dollar operator *(Dollar z, Quantity quantity)
    {
        return new Dollar(z.value * quantity.AsPrimitive());
    }

    /// <summary>
    /// 税額の計算
    /// </summary>
    public static Dollar operator *(Dollar z, TaxRate taxRate)
    {
        return new Dollar(z.value * taxRate.AsPrimitive() / 100);
    }
}
```

**設計意図**：
- 金額計算の型安全性を確保
- 数量や税率との演算を明示的に定義
- 業務固有の計算ロジックをカプセル化

### 数量系値オブジェクト

#### Quantity
- 発注数量、在庫数量を表現
- 負の値を防ぐバリデーションを含む

#### Gram
- 製品重量を表現
- 送料計算で使用

#### Days
- リードタイム、配送日数を表現
- 日付計算で使用

### 識別子値オブジェクト

```plantuml
@startuml
skinparam class {
  BackgroundColor<<ValueObject>> #E3F2FD
}

class ProductId <<ValueObject>>
class VendorId <<ValueObject>>
class PurchaseOrderId <<ValueObject>>
class EmployeeId <<ValueObject>>
class ShipMethodId <<ValueObject>>

note right of ProductId
  各識別子は値オブジェクトとして実装
  • 型安全性の確保
  • 誤った代入の防止
  • Unregistered状態の表現
end note
@enduml
```

### ビジネスルール値オブジェクト

#### OrderStatus
```csharp
public enum OrderStatus
{
    Pending,     // 承認待ち
    Approved,    // 承認済み
    Rejected,    // 差し戻し
    Complete     // 完了
}
```

#### CreditRating
- ベンダーの信用度を1-5の範囲で表現
- 信用度に基づく承認ルールで使用

## エンティティ（Entities）

### Product（製品）

```plantuml
@startuml
skinparam class {
  BackgroundColor<<Entity>> #E8F5E8
}

class Product <<Entity>> {
  - ProductId: ProductId
  - Name: string
  - ProductNumber: string
  - Color: string
  - StandardPrice: Dollar
  - ListPrice: Dollar
  - Weight: Gram
  - ModifiedDateTime: ModifiedDateTime
}

note right of Product
  製品エンティティ
  • ProductIdによる識別
  • 価格と重量の値オブジェクト使用
  • 発注計算で重量が必要
end note
@enduml
```

**設計意図**：
- 製品固有の属性をカプセル化
- 価格と重量を値オブジェクトで型安全に管理
- 発注計算における重量の重要性を反映

### Vendor（ベンダー）

```plantuml
@startuml
skinparam class {
  BackgroundColor<<Entity>> #E8F5E8
}

class Vendor <<Entity>> {
  - VendorId: VendorId
  - AccountNumber: AccountNumber
  - Name: string
  - CreditRating: CreditRating
  - IsPreferredVendor: bool
  - IsActive: bool
  - PurchasingWebServiceUrl: Uri?
  - TaxRate: TaxRate
  - ModifiedDateTime: ModifiedDateTime
  - VendorProducts: IReadOnlyList<VendorProduct>
}

note right of Vendor
  ベンダーエンティティ
  • 信用度による承認制御
  • 税率計算
  • 取扱製品の集約
end note
@enduml
```

**設計意図**：
- ベンダー固有のビジネスルール（信用度、税率）をカプセル化
- 取扱製品との関係を明示
- 優先ベンダーの概念を表現

### VendorProduct（ベンダー製品）

```plantuml
@startuml
skinparam class {
  BackgroundColor<<Entity>> #E8F5E8
}

class VendorProduct <<Entity>> {
  - ProductId: ProductId
  - VendorId: VendorId
  - AverageLeadTime: Days
  - StandardPrice: Dollar
  - OnOrderQuantity: Quantity
  - UnitMeasureCode: UnitMeasureCode
  - ModifiedDateTime: ModifiedDateTime
}

note right of VendorProduct
  ベンダー固有の製品情報
  • リードタイム管理
  • ベンダー別価格
  • 発注中数量の追跡
end note
@enduml
```

**設計意図**：
- ベンダー毎の製品固有情報を管理
- 発注時のリードタイム計算で使用
- 価格とリードタイムのベンダー差を表現

## 集約（Aggregates）

### PurchaseOrder集約

```plantuml
@startuml
skinparam class {
  BackgroundColor<<Aggregate>> #FFE0E0
  BackgroundColor<<Entity>> #E8F5E8
}

class PurchaseOrder <<Aggregate>> {
  - Id: PurchaseOrderId
  - RevisionNumber: RevisionNumber
  - Status: OrderStatus
  - EmployeeId: EmployeeId
  - VendorId: VendorId
  - ShipMethodId: ShipMethodId
  - OrderDate: Date
  - ShipDate: Date?
  - SubTotal: Dollar
  - TaxAmount: Dollar
  - Freight: Dollar
  - TotalDue: Dollar
  - ModifiedDateTime: ModifiedDateTime
  - Details: IReadOnlyList<PurchaseOrderDetail>
  + NewOrder(...): PurchaseOrder
}

class PurchaseOrderDetail <<Entity>> {
  - Id: PurchaseOrderDetailId
  - DueDate: Date
  - OrderQuantity: Quantity
  - ProductId: ProductId
  - UnitPrice: Dollar
  - LineTotal: Dollar
  - ReceivedQuantity: Quantity
  - RejectedQuantity: Quantity
  - ModifiedDateTime: ModifiedDateTime
  + NewOrderDetail(...): PurchaseOrderDetail
}

PurchaseOrder *-- PurchaseOrderDetail

note left of PurchaseOrder
  発注集約ルート
  • 整合性の境界
  • トランザクション単位
  • 金額自動計算
end note

note right of PurchaseOrderDetail
  発注明細
  • 集約内エンティティ
  • 外部からの直接操作禁止
  • 行金額の自動計算
end note
@enduml
```

**集約の設計原則**：
1. **整合性境界**: 発注と明細の整合性を保証
2. **トランザクション単位**: 発注全体が一つのトランザクション
3. **集約ルートアクセス**: PurchaseOrderを通じてのみ明細にアクセス
4. **不変条件**: 小計、税額、送料、総額の整合性を維持

### 発注の生成ファクトリメソッド

```csharp
/// <summary>
/// 新規の発注をインスタンス化する。
/// </summary>
public static PurchaseOrder NewOrder(
    EmployeeId employeeId,
    VendorId vendorId,
    ShipMethodId shipMethodId,
    Date orderDate,
    Dollar subTotal,
    Dollar taxAmount,
    Dollar freight,
    IReadOnlyList<PurchaseOrderDetail> details)
{
    return new(
        PurchaseOrderId.Unregistered,
        RevisionNumber.Unregistered,
        OrderStatus.Pending,
        employeeId,
        vendorId,
        shipMethodId,
        orderDate,
        null,
        subTotal,
        taxAmount,
        freight,
        subTotal + taxAmount + freight,  // 総額自動計算
        ModifiedDateTime.Unregistered,
        details);
}
```

**設計意図**：
- 新規発注の初期状態を統一
- 総額計算のビジネスルールを集約
- 未登録状態の明示的表現

## ドメインサービス（Domain Services）

### PurchaseOrderBuilder（発注ビルダー）

```plantuml
@startuml
skinparam class {
  BackgroundColor<<Service>> #FFF3E0
}

class PurchaseOrderBuilder <<Service>> {
  - employeeId: EmployeeId
  - vendor: Vendor
  - shipMethod: ShipMethod
  - orderDate: Date
  - details: IList<(Product, PurchaseOrderDetail)>
  + AddProduct(Product, Quantity): void
  + Build(): PurchaseOrder
}

note right of PurchaseOrderBuilder
  複雑な発注構築ロジック
  • 複数製品の集約
  • 金額計算
  • 重量計算
  • 税額・送料計算
end note
@enduml
```

**実装例**：
```csharp
/// <summary>
/// 発注する商品を追加する。
/// </summary>
public void AddProduct(Product product, Quantity quantity)
{
    var vendorProduct = _vendor
        .VendorProducts
        .Single(x => x.ProductId == product.ProductId);
    _details.Add(
        (
            product,
            PurchaseOrderDetail.NewOrderDetail(
                _orderDate + vendorProduct.AverageLeadTime,  // 納期計算
                product.ProductId,
                vendorProduct.StandardPrice,
                quantity)));
}

/// <summary>
/// 発注をビルドする。
/// </summary>
public PurchaseOrder Build()
{
    Dollar subTotal = _details
        .Sum(x => x.PurchaseOrderDetail.LineTotal);
    Dollar taxAmount = subTotal * _vendor.TaxRate;  // 税額計算

    Gram totalWeight = _details
        .Sum(x => x.Product.Weight * x.PurchaseOrderDetail.OrderQuantity);
    Dollar freight = _shipMethod.ShipRate * totalWeight;  // 送料計算

    return PurchaseOrder.NewOrder(
        _employeeId,
        _vendor.VendorId,
        _shipMethod.ShipMethodId,
        _orderDate,
        subTotal,
        taxAmount,
        freight,
        _details.Select(x => x.PurchaseOrderDetail).ToList()
    );
}
```

**設計意図**：
- 複雑な発注構築ロジックをカプセル化
- ベンダー固有の計算ルール（税率、リードタイム）を適用
- 重量ベースの送料計算を実装

## 再発注サブドメイン

### RequiringPurchaseProduct（要再発注製品）

```plantuml
@startuml
class RequiringPurchaseProduct {
  - VendorId: VendorId
  - VendorName: string
  - ProductCategoryId: ProductCategoryId
  - ProductCategoryName: string
  - ProductSubcategoryId: ProductSubcategoryId
  - ProductSubcategoryName: string
  - ProductId: ProductId
  - ProductName: string
  - PurchasingQuantity: Quantity
  - UnitPrice: Dollar
  - ShipmentResponseDays: Days
  - AverageLeadTime: Days
  - InventoryQuantity: Quantity
  - UnclaimedPurchaseQuantity: Quantity
  - AverageDailyShipmentQuantity: DoubleQuantity

  + LineTotal: Dollar
}

note right of RequiringPurchaseProduct
  クエリ専用ドメインオブジェクト
  • 複数テーブルの結合結果
  • 計算プロパティ含む
  • 読み取り専用
end note
@enduml
```

**設計意図**：
- 複雑な再発注判定ロジックのカプセル化
- 複数エンティティから派生した計算済み情報
- CQRS パターンの Query 側での使用を想定

## リポジトリインターフェース

### IProductRepository
```csharp
public interface IProductRepository
{
    Task<Product?> GetAsync(ProductId id);
    Task<IReadOnlyList<Product>> GetAllAsync();
}
```

### IVendorRepository
```csharp
public interface IVendorRepository
{
    Task<Vendor?> GetAsync(VendorId id);
    Task<IReadOnlyList<Vendor>> GetAllAsync();
}
```

### IPurchaseOrderRepository
```csharp
public interface IPurchaseOrderRepository
{
    Task<PurchaseOrder?> GetAsync(PurchaseOrderId id);
    Task<PurchaseOrderId> AddAsync(PurchaseOrder order);
    Task UpdateAsync(PurchaseOrder order);
    Task<IReadOnlyList<PurchaseOrder>> GetAllAsync();
}
```

### IShipMethodRepository
```csharp
public interface IShipMethodRepository
{
    Task<ShipMethod?> GetAsync(ShipMethodId id);
    Task<IReadOnlyList<ShipMethod>> GetAllAsync();
}
```

## クエリインターフェース

### IRequiringPurchaseProductQuery
```csharp
public interface IRequiringPurchaseProductQuery
{
    Task<IReadOnlyList<RequiringPurchaseProduct>> GetAllAsync();
}
```

## ドメインモデルの利用例

### 発注作成のユースケース

```plantuml
@startuml
participant "PurchasingService" as Service
participant "PurchaseOrderBuilder" as Builder
participant "VendorRepository" as VendorRepo
participant "ProductRepository" as ProductRepo
participant "ShipMethodRepository" as ShipRepo
participant "PurchaseOrderRepository" as OrderRepo

Service -> VendorRepo : GetAsync(vendorId)
Service -> ShipRepo : GetAsync(shipMethodId)
Service -> Builder : new PurchaseOrderBuilder(...)

loop 製品毎
  Service -> ProductRepo : GetAsync(productId)
  Service -> Builder : AddProduct(product, quantity)
end

Service -> Builder : Build()
Builder -> Service : PurchaseOrder
Service -> OrderRepo : AddAsync(order)
@enduml
```

### 金額計算の流れ

```plantuml
@startuml
start
:製品選択;
:VendorProduct から単価とリードタイム取得;
:PurchaseOrderDetail 作成;
note right: LineTotal = UnitPrice * Quantity
:明細を Builder に追加;
:小計計算;
note right: SubTotal = Σ(LineTotal)
:税額計算;
note right: TaxAmount = SubTotal * TaxRate
:重量計算;
note right: TotalWeight = Σ(Weight * Quantity)
:送料計算;
note right: Freight = ShipRate * TotalWeight
:総額計算;
note right: TotalDue = SubTotal + TaxAmount + Freight
:PurchaseOrder 作成;
stop
@enduml
```

## 設計上の考慮事項

### 1. 型安全性の確保
- 識別子を値オブジェクトとして実装
- 金額、数量、重量などを専用型で管理
- 誤った代入や計算ミスを防止

### 2. ビジネスルールの集約
- 金額計算ロジックを Dollar 値オブジェクトに集約
- 発注構築ロジックを PurchaseOrderBuilder に集約
- 集約内での整合性維持

### 3. 拡張性の考慮
- インターフェースによる依存関係の抽象化
- ドメインサービスによる複雑ロジックの分離
- サブドメインによる機能分割

### 4. テスタビリティ
- 純粋関数としての値オブジェクト
- 依存関係注入可能なリポジトリ
- モック化しやすいインターフェース設計

## 今後の拡張ポイント

### 1. ドメインイベント
- 発注承認イベント
- 入荷完了イベント
- 在庫不足警告イベント

### 2. 仕様パターン
- 承認基準の仕様
- 再発注条件の仕様
- ベンダー選択基準

### 3. 戦略パターン
- 送料計算戦略
- 税額計算戦略
- リードタイム計算戦略

## まとめ

本ドメインモデルは以下の特徴を持ちます：

1. **値オブジェクト中心の設計**: 型安全性とビジネスルールの集約
2. **適切な集約境界**: 整合性とトランザクション境界の明確化
3. **ドメインサービスの活用**: 複雑なロジックの分離
4. **拡張可能な設計**: 新機能追加に対する柔軟性

このモデルにより、「変更を楽に安全にできて役に立つソフトウェア」の実現を目指しています。