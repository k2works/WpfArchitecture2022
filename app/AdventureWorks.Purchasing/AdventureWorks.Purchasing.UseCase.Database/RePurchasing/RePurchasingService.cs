using AdventureWorks.Database;
using AdventureWorks.Purchasing.UseCase.RePurchasing;
using Dapper;

namespace AdventureWorks.Purchasing.UseCase.Database.RePurchasing;
public class RePurchasingService : IRePurchasingService
{
    private readonly IDatabase _database;
    private readonly IVendorRepository _vendorRepository;

    public RePurchasingService(IDatabase database, IVendorRepository vendorRepository)
    {
        _database = database;
        _vendorRepository = vendorRepository;
    }

    public async Task<IList<RequiringPurchaseProduct>> GetRequiringPurchaseProductsAsync()
    {
        using var transaction = _database.BeginTransaction();

        var results = await transaction.Connection.QueryAsync<RequiringPurchaseProduct>(
            @"
select
    VendorId,
    VendorName,
    ProductCategoryId,
    ProductCategoryName,
    ProductSubcategoryId,
    ProductSubcategoryName,
    ProductId,
    ProductName,
    PurchasingQuantity,
    UnitPrice,
    ShipmentResponseDays,
    AverageLeadTime,
    InventoryQuantity,
    UnclaimedPurchaseQuantity,
    AverageDailyShipmentQuantity
from
    RePurchasing.vProductRequiringPurchase");

        return results.ToList();
    }

    public async Task<Vendor> GetVendorAsync(VendorId vendorId)
    {
        return await _vendorRepository.GetVendorByIdAsync(vendorId);
    }
}
