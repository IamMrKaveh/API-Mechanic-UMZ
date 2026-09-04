using Domain.Brand.Events;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.User.ValueObjects;

namespace Tests.Domain.Brand.Events;

public class BrandEventsTests
{
    private static BrandId NewBrandId() => BrandId.NewId();

    private static BrandName NewName() => BrandName.Create("Sony");

    private static CategoryId NewCategoryId() => CategoryId.NewId();

    [Fact]
    public void BrandCreatedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var id = NewBrandId();
        var name = NewName();
        var slug = BrandSlug.Create("sony");
        var categoryId = NewCategoryId();

        var sut = new BrandCreatedEvent(id, name, slug, categoryId);

        sut.BrandId.ShouldBe(id);
        sut.Name.ShouldBe(name);
        sut.Slug.ShouldBe(slug);
        sut.CategoryId.ShouldBe(categoryId);
    }

    [Fact]
    public void BrandActivatedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var id = NewBrandId();
        var name = NewName();
        var categoryId = NewCategoryId();

        var sut = new BrandActivatedEvent(id, name, categoryId);

        sut.BrandId.ShouldBe(id);
        sut.Name.ShouldBe(name);
        sut.CategoryId.ShouldBe(categoryId);
    }

    [Fact]
    public void BrandDeactivatedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var id = NewBrandId();
        var name = NewName();
        var categoryId = NewCategoryId();

        var sut = new BrandDeactivatedEvent(id, name, categoryId);

        sut.BrandId.ShouldBe(id);
        sut.Name.ShouldBe(name);
        sut.CategoryId.ShouldBe(categoryId);
    }

    [Fact]
    public void BrandCategoryChangedEvent_ExposesPreviousAndNewCategory()
    {
        var id = NewBrandId();
        var previous = NewCategoryId();
        var next = NewCategoryId();

        var sut = new BrandCategoryChangedEvent(id, previous, next);

        sut.BrandId.ShouldBe(id);
        sut.PreviousCategoryId.ShouldBe(previous);
        sut.NewCategoryId.ShouldBe(next);
        sut.PreviousCategoryId.ShouldNotBe(sut.NewCategoryId);
    }

    [Fact]
    public void BrandDeletedEvent_WithDeleter_StoresDeleter()
    {
        var deleter = UserId.NewId();

        var sut = new BrandDeletedEvent(NewBrandId(), NewName(), NewCategoryId(), deleter);

        sut.DeletedBy.ShouldBe(deleter);
    }

    [Fact]
    public void BrandDeletedEvent_WithoutDeleter_StoresNull()
    {
        var sut = new BrandDeletedEvent(NewBrandId(), NewName(), NewCategoryId(), null);

        sut.DeletedBy.ShouldBeNull();
    }

    [Fact]
    public void BrandUpdatedEvent_WithDescription_StoresIt()
    {
        var sut = new BrandUpdatedEvent(
            NewBrandId(), NewName(), BrandSlug.Create("sony"), "Official store");

        sut.Description.ShouldBe("Official store");
    }

    [Fact]
    public void BrandUpdatedEvent_WithoutDescription_StoresNull()
    {
        var sut = new BrandUpdatedEvent(
            NewBrandId(), NewName(), BrandSlug.Create("sony"), null);

        sut.BrandId.ShouldNotBe(BrandId.NewId());
        sut.Description.ShouldBeNull();
    }
}
