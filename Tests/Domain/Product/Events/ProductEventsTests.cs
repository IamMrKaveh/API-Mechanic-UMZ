using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Product.Events;
using Domain.Product.ValueObjects;

namespace Tests.Domain.Product.Events;

public class ProductEventsTests
{
    [Fact]
    public void ProductCreatedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var productId = ProductId.NewId();
        var name = ProductName.Create("Nike Air Max");
        var brandId = BrandId.NewId();
        var categoryId = CategoryId.NewId();

        var sut = new ProductCreatedEvent(productId, name, brandId, categoryId);

        sut.ProductId.ShouldBe(productId);
        sut.ProductName.ShouldBe(name);
        sut.BrandId.ShouldBe(brandId);
        sut.CategoryId.ShouldBe(categoryId);
    }

    [Fact]
    public void ProductUpdatedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var productId = ProductId.NewId();
        var name = ProductName.Create("Renamed");
        var slug = ProductSlug.Create("renamed");

        var sut = new ProductUpdatedEvent(productId, name, slug, "new desc");

        sut.ProductId.ShouldBe(productId);
        sut.ProductName.ShouldBe(name);
        sut.Slug.ShouldBe(slug);
        sut.Description.ShouldBe("new desc");
    }

    [Fact]
    public void ProductBrandChangedEvent_ExposesPreviousAndNewBrandIds()
    {
        var productId = ProductId.NewId();
        var previous = BrandId.NewId();
        var next = BrandId.NewId();

        var sut = new ProductBrandChangedEvent(productId, previous, next);

        sut.ProductId.ShouldBe(productId);
        sut.PreviousBrandId.ShouldBe(previous);
        sut.NewBrandId.ShouldBe(next);
    }

    [Fact]
    public void ProductCategoryChangedEvent_ExposesPreviousAndNewCategoryIds()
    {
        var productId = ProductId.NewId();
        var previous = CategoryId.NewId();
        var next = CategoryId.NewId();

        var sut = new ProductCategoryChangedEvent(productId, previous, next);

        sut.ProductId.ShouldBe(productId);
        sut.PreviousCategoryId.ShouldBe(previous);
        sut.NewCategoryId.ShouldBe(next);
    }

    [Fact]
    public void ProductActivatedEvent_ExposesProductId()
    {
        var id = ProductId.NewId();

        new ProductActivatedEvent(id).ProductId.ShouldBe(id);
    }

    [Fact]
    public void ProductDeactivatedEvent_ExposesProductId()
    {
        var id = ProductId.NewId();

        new ProductDeactivatedEvent(id).ProductId.ShouldBe(id);
    }
}
