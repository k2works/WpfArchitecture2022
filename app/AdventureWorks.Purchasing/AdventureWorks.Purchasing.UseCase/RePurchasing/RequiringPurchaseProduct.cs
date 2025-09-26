using AdventureWorks.Purchasing.Production;

namespace AdventureWorks.Purchasing.UseCase.RePurchasing;

public record RequiringPurchaseProduct
{
    public VendorId VendorId { get; init; } = default!;
    public string VendorName { get; init; } = string.Empty;
    public ProductCategoryId ProductCategoryId { get; init; } = default!;
    public string ProductCategoryName { get; init; } = string.Empty;
    public ProductSubcategoryId ProductSubcategoryId { get; init; } = default!;
    public string ProductSubcategoryName { get; init; } = string.Empty;
    public ProductId ProductId { get; init; } = default!;
    public string ProductName { get; init; } = string.Empty;
    public Quantity PurchasingQuantity { get; init; } = default!;
    public Days ShipmentResponseDays { get; init; } = default!;
    public Days AverageLeadTime { get; init; } = default!;
    public Quantity InventoryQuantity { get; init; } = default!;
    public Quantity UnclaimedPurchaseQuantity { get; init; } = default!;
    public DoubleQuantity AverageDailyShipmentQuantity { get; init; } = default!;
    public Dollar UnitPrice { get; init; } = default!;
    public Dollar LineTotal => UnitPrice * PurchasingQuantity;
}