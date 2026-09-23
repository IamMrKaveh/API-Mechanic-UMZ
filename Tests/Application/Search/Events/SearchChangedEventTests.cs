using Application.Search.Contracts;
using Application.Search.Events;
using Application.Search.Features.Shared;
using SharedKernel.Enums;

namespace Tests.Application.Search.Events;

public class SearchChangedEventTests
{
    [Fact]
    public void ProductChangedEvent_ExposesBrandEntityTypeAndDocument()
    {
        var doc = new BrandSearchDocument { BrandId = Guid.NewGuid(), Name = "Bosch" };

        var e = new BrandChangedEvent(42, EntityChangeType.Updated, doc);

        e.EntityId.ShouldBe(42);
        e.ChangeType.ShouldBe(EntityChangeType.Updated);
        e.Document.ShouldBeSameAs(doc);
        e.EntityType.ShouldBe("Brand");
        e.ShouldBeAssignableTo<IEntityChangeEvent>();
    }

    [Fact]
    public void CategoryChangedEvent_ExposesCategoryEntityType()
    {
        var e = new CategoryChangedEvent(7, EntityChangeType.Created);

        e.EntityId.ShouldBe(7);
        e.EntityType.ShouldBe("Category");
        e.Document.ShouldBeNull();
        e.ShouldBeAssignableTo<IEntityChangeEvent>();
    }

    [Fact]
    public void ProductChangedEvent_ExposesProductEntityType()
    {
        var doc = new ProductSearchDocument { ProductId = Guid.NewGuid(), Name = "Pad" };

        var e = new ProductChangedEvent(9, EntityChangeType.Deleted, doc);

        e.EntityId.ShouldBe(9);
        e.EntityType.ShouldBe("Product");
        e.ChangeType.ShouldBe(EntityChangeType.Deleted);
        e.Document.ShouldBeSameAs(doc);
    }

    [Fact]
    public void Events_ValueEquality_Works()
    {
        new BrandChangedEvent(1, EntityChangeType.Created).ShouldBe(new BrandChangedEvent(1, EntityChangeType.Created));
        new CategoryChangedEvent(1, EntityChangeType.Created).Equals(new BrandChangedEvent(1, EntityChangeType.Created)).ShouldBeFalse();
    }
}
