namespace AdventureWorks.Purchasing.UseCase.RePurchasing;

public interface IRePurchasingService
{
    Task<IList<RequiringPurchaseProduct>> GetRequiringPurchaseProductsAsync();
    Task<Vendor> GetVendorAsync(VendorId vendorId);
}