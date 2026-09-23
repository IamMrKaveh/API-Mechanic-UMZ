using Application.Wallet.Features.Shared;

namespace Tests.Application.Wallet.Features.Shared;

public class WalletOverviewFilterTests
{
    [Fact]
    public void Defaults_AllNull()
    {
        var filter = new WalletOverviewFilter();

        filter.Search.ShouldBeNull();
        filter.IsFrozen.ShouldBeNull();
        filter.MinBalance.ShouldBeNull();
        filter.MaxBalance.ShouldBeNull();
        filter.CreatedFrom.ShouldBeNull();
        filter.CreatedTo.ShouldBeNull();
        filter.SortBy.ShouldBeNull();
    }

    [Fact]
    public void InitProperties_RoundTrip()
    {
        var filter = new WalletOverviewFilter
        {
            Search = "0912",
            IsFrozen = true,
            MinBalance = 0m,
            MaxBalance = 1_000_000m,
            CreatedFrom = new DateTime(2026, 1, 1),
            CreatedTo = new DateTime(2026, 3, 1),
            SortBy = "balance_desc"
        };

        filter.Search.ShouldBe("0912");
        filter.IsFrozen.ShouldBe(true);
        filter.SortBy.ShouldBe("balance_desc");
    }

    [Fact]
    public void WithExpression_PreservesOthers()
    {
        var filter = new WalletOverviewFilter { Search = "x", IsFrozen = false };

        var updated = filter with { IsFrozen = true };

        updated.IsFrozen.ShouldBe(true);
        updated.Search.ShouldBe("x");
    }
}
