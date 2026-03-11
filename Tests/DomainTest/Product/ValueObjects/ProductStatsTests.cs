namespace Tests.DomainTest.Product.ValueObjects;

public class ProductStatsTests
{
    [Fact]
    public void CreateEmpty_ShouldReturnStatsWithZeroValues()
    {
        var stats = ProductStats.CreateEmpty();

        stats.TotalStock.Should().Be(0);
        stats.AverageRating.Should().Be(0);
        stats.ReviewCount.Should().Be(0);
        stats.SalesCount.Should().Be(0);
        stats.MinPrice.Amount.Should().Be(0);
        stats.MaxPrice.Amount.Should().Be(0);
    }

    [Fact]
    public void UpdateReviews_ShouldReturnNewStatsWithUpdatedReviews()
    {
        var stats = ProductStats.CreateEmpty();

        var updated = stats.UpdateReviews(count: 10, average: 4.5m);

        updated.ReviewCount.Should().Be(10);
        updated.AverageRating.Should().Be(4.5m);
    }

    [Fact]
    public void UpdateReviews_ShouldPreserveOtherValues()
    {
        var stats = ProductStats.CreateEmpty();

        var updated = stats.UpdateReviews(count: 10, average: 4.5m);

        updated.TotalStock.Should().Be(stats.TotalStock);
        updated.SalesCount.Should().Be(stats.SalesCount);
    }

    [Fact]
    public void TwoEmptyStats_ShouldBeEqual()
    {
        var stats1 = ProductStats.CreateEmpty();
        var stats2 = ProductStats.CreateEmpty();

        stats1.Should().Be(stats2);
    }

    [Fact]
    public void TwoStats_WithDifferentSalesCounts_ShouldNotBeEqual()
    {
        var stats1 = ProductStats.CreateEmpty();
        var stats2 = stats1.UpdateReviews(10, 4.5m);

        stats1.Should().NotBe(stats2);
    }
}