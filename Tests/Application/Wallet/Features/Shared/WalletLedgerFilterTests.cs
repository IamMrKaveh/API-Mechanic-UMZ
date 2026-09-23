using Application.Wallet.Features.Shared;

namespace Tests.Application.Wallet.Features.Shared;

public class WalletLedgerFilterTests
{
    [Fact]
    public void Defaults_MaxRowsIsTenThousandAndFiltersNull()
    {
        var filter = new WalletLedgerFilter();

        filter.MaxRows.ShouldBe(10_000);
        filter.FromDate.ShouldBeNull();
        filter.ToDate.ShouldBeNull();
        filter.TransactionType.ShouldBeNull();
        filter.MinAmount.ShouldBeNull();
        filter.MaxAmount.ShouldBeNull();
        filter.SearchTerm.ShouldBeNull();
    }

    [Fact]
    public void InitProperties_RoundTrip()
    {
        var filter = new WalletLedgerFilter
        {
            FromDate = new DateTime(2026, 1, 1),
            ToDate = new DateTime(2026, 2, 1),
            TransactionType = "Credit",
            MinAmount = 1_000m,
            MaxAmount = 50_000m,
            SearchTerm = "topup",
            MaxRows = 500
        };

        filter.TransactionType.ShouldBe("Credit");
        filter.MinAmount.ShouldBe(1_000m);
        filter.MaxRows.ShouldBe(500);
    }
}
