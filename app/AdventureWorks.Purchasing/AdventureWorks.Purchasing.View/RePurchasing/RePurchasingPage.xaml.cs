using AdventureWorks;
using AdventureWorks.Purchasing.UseCase.RePurchasing;
using AdventureWorks.Purchasing.ViewModel.RePurchasing;

namespace AdventureWorks.Purchasing.View.RePurchasing;
/// <summary>
/// RePurchasingPage.xaml の相互作用ロジック
/// </summary>
public partial class RePurchasingPage
{
    public RePurchasingPage()
    {
        InitializeComponent();
    }
}

public class RePurchasingDesignViewModel : RePurchasingViewModel
{
    public RePurchasingDesignViewModel() : base(default!, default!, default!)
    {
    }

    // デザインタイム用のダミープロパティを追加
    public new Vendor Vendor => new(
        default!,
        default!,
        "Sample Vendor Co.",
        default!,
        true,
        true,
        null,
        default!,
        default!,
        Array.Empty<VendorProduct>()
    );
}