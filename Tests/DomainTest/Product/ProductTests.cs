using Domain.Attribute.Entities;

namespace Tests.DomainTest.Product;

public class ProductTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldReturnActiveProduct()
    {
        var product = new ProductBuilder().Build();

        product.Should().NotBeNull();
        product.IsActive.Should().BeTrue();
        product.IsDeleted.Should().BeFalse();
        product.IsFeatured.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldSetCreatedAt()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var product = new ProductBuilder().Build();

        product.CreatedAt.Should().BeAfter(before);
    }

    [Fact]
    public void Create_ShouldRaiseProductCreatedEvent()
    {
        var product = new ProductBuilder().Build();

        product.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "ProductCreatedEvent");
    }

    [Fact]
    public void Create_ShouldHaveEmptyStats()
    {
        var product = new ProductBuilder().Build();

        product.Stats.TotalStock.Should().Be(0);
        product.Stats.SalesCount.Should().Be(0);
        product.Stats.ReviewCount.Should().Be(0);
    }

    [Fact]
    public void Create_ShouldHaveNoVariants()
    {
        var product = new ProductBuilder().Build();

        product.Variants.Should().BeEmpty();
    }

    [Fact]
    public void Activate_WhenInactive_ShouldSetActiveToTrue()
    {
        var product = new ProductBuilder().Build();
        product.Deactivate();

        product.Activate();

        product.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ShouldNotRaiseDomainEvent()
    {
        var product = new ProductBuilder().Build();
        product.ClearDomainEvents();

        product.Activate();

        product.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Activate_ShouldRaiseProductActivatedEvent()
    {
        var product = new ProductBuilder().Build();
        product.Deactivate();
        product.ClearDomainEvents();

        product.Activate();

        product.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "ProductActivatedEvent");
    }

    [Fact]
    public void Deactivate_WhenActive_ShouldSetActiveToFalse()
    {
        var product = new ProductBuilder().Build();

        product.Deactivate();

        product.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ShouldNotRaiseDomainEvent()
    {
        var product = new ProductBuilder().Build();
        product.Deactivate();
        product.ClearDomainEvents();

        product.Deactivate();

        product.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Deactivate_ShouldRaiseProductDeactivatedEvent()
    {
        var product = new ProductBuilder().Build();
        product.ClearDomainEvents();

        product.Deactivate();

        product.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "ProductDeactivatedEvent");
    }

    [Fact]
    public void Delete_ShouldMarkProductAsDeleted()
    {
        var product = new ProductBuilder().Build();

        product.Delete(deletedBy: 1);

        product.IsDeleted.Should().BeTrue();
        product.DeletedBy.Should().Be(1);
        product.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Delete_ShouldRaiseProductDeletedEvent()
    {
        var product = new ProductBuilder().Build();
        product.ClearDomainEvents();

        product.Delete(deletedBy: 1);

        product.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "ProductDeletedEvent");
    }

    [Fact]
    public void Restore_WhenDeleted_ShouldMarkAsNotDeletedAndActive()
    {
        var product = new ProductBuilder().Build();
        product.Delete(1);

        product.Restore();

        product.IsDeleted.Should().BeFalse();
        product.IsActive.Should().BeTrue();
        product.DeletedAt.Should().BeNull();
        product.DeletedBy.Should().BeNull();
    }

    [Fact]
    public void UpdateDetails_ShouldUpdateNameAndDescription()
    {
        var product = new ProductBuilder().Build();

        product.UpdateDetails("نام جدید", "توضیح جدید", null, 1, true);

        product.Name.Value.Should().Be("نام جدید");
        product.Description.Should().Be("توضیح جدید");
    }

    [Fact]
    public void UpdateDetails_ShouldRaiseProductUpdatedEvent()
    {
        var product = new ProductBuilder().Build();
        product.ClearDomainEvents();

        product.UpdateDetails("نام جدید", "توضیح جدید", null, 1, true);

        product.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "ProductUpdatedEvent");
    }

    [Fact]
    public void AddVariant_ShouldAddVariantToCollection()
    {
        var product = new ProductBuilder().Build();

        product.AddVariant(null, 100, 150, 200, 10, false, 1.0m, new List<AttributeValue>());

        product.Variants.Should().HaveCount(1);
    }

    [Fact]
    public void FindVariant_WhenExists_ShouldReturnVariant()
    {
        var product = new ProductBuilder().Build();
        var variant = product.AddVariant(null, 100, 150, 200, 10, false, 1.0m, new List<AttributeValue>());

        var found = product.FindVariant(variant.Id);

        found.Should().NotBeNull();
        found!.Id.Should().Be(variant.Id);
    }

    [Fact]
    public void FindVariant_WhenNotExists_ShouldReturnNull()
    {
        var product = new ProductBuilder().Build();

        var found = product.FindVariant(999);

        found.Should().BeNull();
    }

    [Fact]
    public void UpdateStats_ShouldUpdateProductStats()
    {
        var product = new ProductBuilder().Build();
        var newStats = ProductStats.CreateEmpty();

        product.UpdateStats(newStats);

        product.Stats.Should().Be(newStats);
    }
}