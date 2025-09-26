using System.Collections.ObjectModel;
using AdventureWorks.Purchasing.UseCase.RePurchasing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kamishibai;

namespace AdventureWorks.Purchasing.ViewModel.RePurchasing;

[Navigate]
[INotifyPropertyChanged]
public partial class RequiringPurchaseProductsViewModel : INavigatedAsyncAware
{
    private readonly IRePurchasingService _rePurchasingService;
    private readonly IPresentationService _presentationService;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PurchaseCommand))]
    private RequiringPurchaseProduct? _selectedRequiringPurchaseProduct;

    public ObservableCollection<RequiringPurchaseProduct> RequiringPurchaseProducts { get; } = new();

    public RequiringPurchaseProductsViewModel(
        [Inject] IRePurchasingService rePurchasingService, 
        [Inject] IPresentationService presentationService)
    {
        _rePurchasingService = rePurchasingService;
        _presentationService = presentationService;
    }

    public async Task OnNavigatedAsync(PostForwardEventArgs args)
    {
        RequiringPurchaseProducts.Replace(await _rePurchasingService.GetRequiringPurchaseProductsAsync());
    }

    [RelayCommand]
    private Task GoBackAsync() => _presentationService.GoBackAsync();

    [RelayCommand(CanExecute = nameof(CanPurchaseAsync))]
    private async Task PurchaseAsync()
    {
        if (SelectedRequiringPurchaseProduct is null) return;

        // 選択された商品のベンダーを取得
        var vendor = await _rePurchasingService.GetVendorAsync(SelectedRequiringPurchaseProduct.VendorId);

        // 同じベンダーの全商品を取得
        var vendorProducts = RequiringPurchaseProducts
            .Where(p => p.VendorId.AsPrimitive() == SelectedRequiringPurchaseProduct.VendorId.AsPrimitive())
            .ToList();

        // RePurchasingページに遷移
        await _presentationService.NavigateToRePurchasingAsync(vendor, vendorProducts);
    }

    private bool CanPurchaseAsync()
    {
        return SelectedRequiringPurchaseProduct is not null;
    }
}