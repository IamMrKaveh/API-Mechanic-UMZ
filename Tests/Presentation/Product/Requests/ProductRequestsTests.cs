using Application.Product.Features.Shared;
using Presentation.Product.Requests;

namespace Tests.Presentation.Product.Requests;

public class ProductRequestsTests
{
    [Fact]
    public void GetProductsRequest_WithDefaults_SetsCorrectly()
    {
        var request = new GetProductsRequest();

        request.Page.ShouldBe(1);
        request.PageSize.ShouldBe(10);
        request.Search.ShouldBeNull();
        request.CategoryId.ShouldBeNull();
        request.BrandId.ShouldBeNull();
        request.MinPrice.ShouldBeNull();
        request.MaxPrice.ShouldBeNull();
        request.InStockOnly.ShouldBeFalse();
        request.SortBy.ShouldBeNull();
    }

    [Fact]
    public void GetProductsRequest_WithAllParameters_SetsCorrectly()
    {
        var categoryId = Guid.NewGuid();
        var brandId = Guid.NewGuid();

        var request = new GetProductsRequest(2, 25, "phone", categoryId, brandId, 100, 1000, true, "Price");

        request.Page.ShouldBe(2);
        request.PageSize.ShouldBe(25);
        request.Search.ShouldBe("phone");
        request.CategoryId.ShouldBe(categoryId);
        request.BrandId.ShouldBe(brandId);
        request.MinPrice.ShouldBe(100);
        request.MaxPrice.ShouldBe(1000);
        request.InStockOnly.ShouldBeTrue();
        request.SortBy.ShouldBe("Price");
    }

    [Fact]
    public void GetAdminProductsRequest_WithDefaults_SetsCorrectly()
    {
        var request = new GetAdminProductsRequest();

        request.Page.ShouldBe(1);
        request.PageSize.ShouldBe(10);
        request.Search.ShouldBeNull();
        request.CategoryId.ShouldBeNull();
        request.BrandId.ShouldBeNull();
        request.IsActive.ShouldBeNull();
        request.IncludeDeleted.ShouldBeFalse();
    }

    [Fact]
    public void GetProductCatalogRequest_WithDefaults_SetsCorrectly()
    {
        var request = new GetProductCatalogRequest();

        request.Page.ShouldBe(1);
        request.PageSize.ShouldBe(10);
        request.InStockOnly.ShouldBeFalse();
        request.IsFeatured.ShouldBeNull();
    }

    [Fact]
    public void CreateProductRequest_WithAllParameters_SetsCorrectly()
    {
        var categoryId = Guid.NewGuid();
        var brandId = Guid.NewGuid();

        var request = new CreateProductRequest("P1", categoryId, brandId);

        request.Name.ShouldBe("P1");
        request.CategoryId.ShouldBe(categoryId);
        request.BrandId.ShouldBe(brandId);
    }

    [Fact]
    public void UpdateProductRequest_WithAllParameters_SetsCorrectly()
    {
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var brandId = Guid.NewGuid();

        var request = new UpdateProductRequest(productId, categoryId, brandId, "P1", "p1", "Desc", true, true, "rv");

        request.ProductId.ShouldBe(productId);
        request.Name.ShouldBe("P1");
        request.Slug.ShouldBe("p1");
        request.IsActive.ShouldBeTrue();
        request.IsFeatured.ShouldBeTrue();
        request.RowVersion.ShouldBe("rv");
    }

    [Fact]
    public void UpdateProductDetailsRequest_WithAllParameters_SetsCorrectly()
    {
        var productId = Guid.NewGuid();
        var brandId = Guid.NewGuid();

        var request = new UpdateProductDetailsRequest(productId, "P1", "Desc", brandId, true, "SKU1", "rv");

        request.ProductId.ShouldBe(productId);
        request.Name.ShouldBe("P1");
        request.BrandId.ShouldBe(brandId);
        request.Sku.ShouldBe("SKU1");
        request.RowVersion.ShouldBe("rv");
    }

    [Fact]
    public void BulkUpdatePricesRequest_WithUpdates_SetsCorrectly()
    {
        var updates = new List<VariantPriceUpdateInput>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), 100, 120)
        };

        var request = new BulkUpdatePricesRequest(updates);

        request.Updates.Count.ShouldBe(1);
    }

    [Fact]
    public void GetProductsRequest_IsRecord_EqualityWorks()
    {
        var request1 = new GetProductsRequest(Search: "phone");
        var request2 = new GetProductsRequest(Search: "phone");
        var request3 = new GetProductsRequest(Search: "laptop");

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }
}
