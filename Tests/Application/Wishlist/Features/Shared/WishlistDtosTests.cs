using Application.Wishlist.Features.Shared;

namespace Tests.Application.Wishlist.Features.Shared;

public class WishlistDtosTests
{
    [Fact]
    public void WishlistItemDto_StoresAllPositions()
    {
        var id = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var addedAt = new DateTime(2026, 1, 10);

        var dto = new WishlistItemDto(id, productId, "Brake Pad", 120_000m, true, "https://cdn/i.png", addedAt);

        dto.Id.ShouldBe(id);
        dto.ProductId.ShouldBe(productId);
        dto.ProductName.ShouldBe("Brake Pad");
        dto.MinPrice.ShouldBe(120_000m);
        dto.IsInStock.ShouldBeTrue();
        dto.IconUrl.ShouldBe("https://cdn/i.png");
        dto.AddedAt.ShouldBe(addedAt);
    }

    [Fact]
    public void WishlistItemDto_AllowsNullIconUrl()
    {
        var dto = new WishlistItemDto(Guid.NewGuid(), Guid.NewGuid(), "P", 10m, false, null, DateTime.UtcNow);

        dto.IconUrl.ShouldBeNull();
        dto.IsInStock.ShouldBeFalse();
    }

    [Fact]
    public void WishlistItemDto_ValueEquality_Works()
    {
        var addedAt = new DateTime(2026, 1, 1);
        var a = new WishlistItemDto(Guid.NewGuid(), Guid.NewGuid(), "P", 1m, true, null, addedAt);

        a.ShouldBe(a with { });
        a.ShouldNotBe(a with { MinPrice = 2m });
    }

    [Fact]
    public void WishlistItemDto_Deconstructs()
    {
        var dto = new WishlistItemDto(Guid.NewGuid(), Guid.NewGuid(), "P", 5m, true, null, DateTime.UtcNow);

        var (_, _, name, price, inStock, _, _) = dto;

        name.ShouldBe("P");
        price.ShouldBe(5m);
        inStock.ShouldBeTrue();
    }
}
