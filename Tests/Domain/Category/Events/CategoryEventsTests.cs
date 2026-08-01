using Domain.Category.Events;
using Domain.Category.ValueObjects;

namespace Tests.Domain.Category.Events;

public class CategoryEventsTests
{
    [Fact]
    public void CategoryCreatedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var id = CategoryId.NewId();
        var slug = CategorySlug.Create("books");

        var sut = new CategoryCreatedEvent(id, "Books", slug);

        sut.CategoryId.ShouldBe(id);
        sut.Name.ShouldBe("Books");
        sut.Slug.ShouldBe(slug);
    }

    [Fact]
    public void CategoryUpdatedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var id = CategoryId.NewId();
        var slug = CategorySlug.Create("movies");

        var sut = new CategoryUpdatedEvent(id, "Movies", slug, "Films and shows");

        sut.CategoryId.ShouldBe(id);
        sut.Name.ShouldBe("Movies");
        sut.Slug.ShouldBe(slug);
        sut.Description.ShouldBe("Films and shows");
    }

    [Fact]
    public void CategoryUpdatedEvent_WithNullDescription_StoresNull()
    {
        var sut = new CategoryUpdatedEvent(CategoryId.NewId(), "n", CategorySlug.Create("s"), null);

        sut.Description.ShouldBeNull();
    }

    [Fact]
    public void CategoryActivatedEvent_ExposesCategoryId()
    {
        var id = CategoryId.NewId();

        new CategoryActivatedEvent(id).CategoryId.ShouldBe(id);
    }

    [Fact]
    public void CategoryDeactivatedEvent_ExposesCategoryId()
    {
        var id = CategoryId.NewId();

        new CategoryDeactivatedEvent(id).CategoryId.ShouldBe(id);
    }
}
