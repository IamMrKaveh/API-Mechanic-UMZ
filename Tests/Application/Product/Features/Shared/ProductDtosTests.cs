using Application.Product.Features.Shared;
using Application.Variant.Features.Shared;

namespace Tests.Application.Product.Features.Shared;

public class ProductDtosTests
{
    [Fact]
    public void ProductDetailDto_Defaults_AreEmpty()
    {
        var dto = new ProductDetailDto();

        dto.Id.ShouldBe(default(Guid));
        dto.Name.ShouldBe(string.Empty);
        dto.Slug.ShouldBe(string.Empty);
        dto.Description.ShouldBe(string.Empty);
        dto.CategoryName.ShouldBe(string.Empty);
        dto.BrandName.ShouldBe(string.Empty);
        dto.RowVersion.ShouldBeNull();
        dto.PrimaryImageUrl.ShouldBeNull();
        dto.Variants.ShouldNotBeNull();
        dto.Variants.ShouldBeEmpty();
    }

    [Fact]
    public void AdminProductDetailDto_HasDeletedAtAndVariants()
    {
        var dto = new AdminProductDetailDto
        {
            Id = Guid.NewGuid(), Name = "P", DeletedAt = new DateTime(2026, 1, 1),
            Variants = new List<ProductVariantViewDto>()
        };

        dto.DeletedAt.ShouldNotBeNull();
        dto.Variants.ShouldBeEmpty();
    }

    [Fact]
    public void PublicProductDetailDto_RoundTrip()
    {
        var dto = new PublicProductDetailDto
        {
            Id = Guid.NewGuid(), Name = "Oil Filter", Slug = "oil-filter",
            CategoryId = Guid.NewGuid(), BrandId = Guid.NewGuid(),
            IsFeatured = true, PrimaryImageUrl = "https://cdn/p.png"
        };

        dto.IsFeatured.ShouldBeTrue();
        dto.PrimaryImageUrl.ShouldBe("https://cdn/p.png");
    }

    [Fact]
    public void ProductCatalogItemDto_DiscountShape_RoundTrip()
    {
        var dto = new ProductCatalogItemDto
        {
            Id = Guid.NewGuid(), Name = "Pad", MinPrice = 100m, MaxPrice = 150m,
            OriginalPrice = 200m, HasDiscount = true, DiscountPercentage = 25,
            TotalStock = 40, HasStock = true
        };

        dto.HasDiscount.ShouldBeTrue();
        dto.DiscountPercentage.ShouldBe(25);
    }

    [Fact]
    public void ProductCatalogSearchParams_StoresPagingAndFilters()
    {
        var search = new ProductCatalogSearchParams(2, 20, "brake", Guid.NewGuid(), null, 10m, 99m, true, "price", true, false);

        search.Page.ShouldBe(2);
        search.PageSize.ShouldBe(20);
        search.Search.ShouldBe("brake");
        search.InStockOnly.ShouldBeTrue();
        search.HasDiscount.ShouldBe(false);
    }

    [Fact]
    public void ProductListItemDto_RoundTrip()
    {
        var dto = new ProductListItemDto
        {
            Id = Guid.NewGuid(), Name = "N", Slug = "n",
            CategoryId = Guid.NewGuid(), BrandId = Guid.NewGuid(),
            IsActive = true, MinPrice = 50m, HasStock = true, TotalStock = 5,
            CreatedAt = new DateTime(2026, 3, 1)
        };

        dto.MinPrice.ShouldBe(50m);
        dto.TotalStock.ShouldBe(5);
    }

    [Fact]
    public void VariantPriceUpdateInput_StoresValues()
    {
        var input = new VariantPriceUpdateInput(Guid.NewGuid(), Guid.NewGuid(), 120m, 150m);

        input.SellingPrice.ShouldBe(120m);
        input.OriginalPrice.ShouldBe(150m);
    }
}
