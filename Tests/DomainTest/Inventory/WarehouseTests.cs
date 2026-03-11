namespace Tests.DomainTest.Inventory;

public class WarehouseTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldReturnActiveWarehouse()
    {
        var warehouse = new WarehouseBuilder().Build();

        warehouse.Should().NotBeNull();
        warehouse.IsActive.Should().BeTrue();
        warehouse.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldSetCreatedAt()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var warehouse = new WarehouseBuilder().Build();

        warehouse.CreatedAt.Should().BeAfter(before);
    }

    [Fact]
    public void Create_ShouldRaiseWarehouseCreatedEvent()
    {
        var warehouse = new WarehouseBuilder().Build();

        warehouse.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "WarehouseCreatedEvent");
    }

    [Fact]
    public void Create_ShouldTrimName()
    {
        var warehouse = new WarehouseBuilder().WithName("  انبار مرکزی  ").Build();

        warehouse.Name.Should().Be("انبار مرکزی");
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrowException()
    {
        var act = () => new WarehouseBuilder().WithName("").Build();

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Activate_WhenInactive_ShouldSetActiveToTrue()
    {
        var warehouse = new WarehouseBuilder().Build();
        warehouse.Deactivate();

        warehouse.Activate();

        warehouse.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Activate_ShouldRaiseWarehouseActivatedEvent()
    {
        var warehouse = new WarehouseBuilder().Build();
        warehouse.Deactivate();
        warehouse.ClearDomainEvents();

        warehouse.Activate();

        warehouse.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "WarehouseActivatedEvent");
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ShouldNotRaiseDomainEvent()
    {
        var warehouse = new WarehouseBuilder().Build();
        warehouse.ClearDomainEvents();

        warehouse.Activate();

        warehouse.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Deactivate_WhenActive_ShouldSetActiveToFalse()
    {
        var warehouse = new WarehouseBuilder().Build();

        warehouse.Deactivate();

        warehouse.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_ShouldRaiseWarehouseDeactivatedEvent()
    {
        var warehouse = new WarehouseBuilder().Build();
        warehouse.ClearDomainEvents();

        warehouse.Deactivate();

        warehouse.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "WarehouseDeactivatedEvent");
    }

    [Fact]
    public void SetAsDefault_ShouldMarkAsDefault()
    {
        var warehouse = new WarehouseBuilder().Build();

        warehouse.SetAsDefault();

        warehouse.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void SetAsDefault_ShouldRaiseWarehouseSetAsDefaultEvent()
    {
        var warehouse = new WarehouseBuilder().Build();
        warehouse.ClearDomainEvents();

        warehouse.SetAsDefault();

        warehouse.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "WarehouseSetAsDefaultEvent");
    }

    [Fact]
    public void Delete_ShouldMarkWarehouseAsDeleted()
    {
        var warehouse = new WarehouseBuilder().Build();

        warehouse.Delete(deletedBy: 1);

        warehouse.IsDeleted.Should().BeTrue();
        warehouse.IsActive.Should().BeFalse();
        warehouse.DeletedBy.Should().Be(1);
    }

    [Fact]
    public void Delete_WhenDefaultWarehouse_ShouldThrowDomainException()
    {
        var warehouse = new WarehouseBuilder().AsDefault().Build();

        var act = () => warehouse.Delete();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Delete_WhenAlreadyDeleted_ShouldNotChangeState()
    {
        var warehouse = new WarehouseBuilder().Build();
        warehouse.Delete(deletedBy: 1);
        var deletedAt = warehouse.DeletedAt;

        warehouse.Delete(deletedBy: 2);

        warehouse.DeletedBy.Should().Be(1);
    }

    [Fact]
    public void Update_ShouldUpdateProperties()
    {
        var warehouse = new WarehouseBuilder().Build();

        warehouse.Update("انبار جنوب", "شیراز", "خیابان زند", null, 2);

        warehouse.Name.Should().Be("انبار جنوب");
        warehouse.City.Should().Be("شیراز");
        warehouse.Priority.Should().Be(2);
    }

    [Fact]
    public void Update_WhenDeleted_ShouldThrowDomainException()
    {
        var warehouse = new WarehouseBuilder().Build();
        warehouse.Delete();

        var act = () => warehouse.Update("انبار جدید", "تهران", null, null, 0);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void GetOrCreateStock_WhenNotExists_ShouldCreateNewStock()
    {
        var warehouse = new WarehouseBuilder().Build();

        var stock = warehouse.GetOrCreateStock(variantId: 1);

        stock.Should().NotBeNull();
        stock.VariantId.Should().Be(1);
        warehouse.Stocks.Should().HaveCount(1);
    }

    [Fact]
    public void GetOrCreateStock_WhenAlreadyExists_ShouldReturnExistingStock()
    {
        var warehouse = new WarehouseBuilder().Build();
        var first = warehouse.GetOrCreateStock(variantId: 1);

        var second = warehouse.GetOrCreateStock(variantId: 1);

        second.Should().BeSameAs(first);
        warehouse.Stocks.Should().HaveCount(1);
    }

    [Fact]
    public void GetAvailableStock_WhenNoStockForVariant_ShouldReturnZero()
    {
        var warehouse = new WarehouseBuilder().Build();

        var available = warehouse.GetAvailableStock(variantId: 1);

        available.Should().Be(0);
    }

    [Fact]
    public void CanFulfill_WhenNotEnoughStock_ShouldReturnFalse()
    {
        var warehouse = new WarehouseBuilder().Build();

        warehouse.CanFulfill(variantId: 1, quantity: 5).Should().BeFalse();
    }

    [Fact]
    public void CanFulfill_WhenEnoughStock_ShouldReturnTrue()
    {
        var warehouse = new WarehouseBuilder().Build();
        var stock = warehouse.GetOrCreateStock(variantId: 1);
        stock.AddStock(10);

        warehouse.CanFulfill(variantId: 1, quantity: 5).Should().BeTrue();
    }
}