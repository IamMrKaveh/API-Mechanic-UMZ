using SharedKernel.Abstractions.Interfaces;
using SharedKernel.Exceptions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.Inventory.Aggregates;

public class WarehouseTests
{
    [Fact]
    public void Create_WithValidInputs_ReturnsInitializedWarehouse()
    {
        var sut = new WarehouseBuilder()
            .WithCode("wh-01")
            .WithName("Main")
            .WithCity("Tehran")
            .WithAddress("Blvd 1")
            .WithPhone("021-11")
            .WithPriority(10)
            .Build();

        sut.ShouldNotBeNull();
        sut.Id.ShouldNotBeNull();
        sut.Id.Value.ShouldNotBe(Guid.Empty);
        sut.Code.Value.ShouldBe("WH-01");
        sut.Name.ShouldBe("Main");
        sut.City.ShouldBe("Tehran");
        sut.Address.ShouldBe("Blvd 1");
        sut.Phone.ShouldBe("021-11");
        sut.Priority.ShouldBe(10);
        sut.IsActive.ShouldBeTrue();
        sut.IsDefault.ShouldBeFalse();
        sut.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public void Create_SetsCreatedAtCloseToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var sut = new WarehouseBuilder().Build();

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.CreatedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void Create_AsDefault_ReturnsWarehouseWithIsDefaultTrue()
    {
        var sut = new WarehouseBuilder().AsDefault().Build();

        sut.IsDefault.ShouldBeTrue();
    }

    [Fact]
    public void Create_ImplementsIActivatableAndIAuditable()
    {
        var sut = new WarehouseBuilder().Build();

        sut.ShouldBeAssignableTo<IActivatable>();
        sut.ShouldBeAssignableTo<IAuditable>();
    }

    [Fact]
    public void Create_WithInvalidCode_PropagatesDomainExceptionFromWarehouseCode()
    {
        Should.Throw<DomainException>(() => new WarehouseBuilder().WithCode("").Build());
    }

    [Fact]
    public void Update_WithNewValues_AppliesThemAndSetsUpdatedAt()
    {
        var sut = new WarehouseBuilder().Build();

        sut.Update("New", "Shiraz", "Addr 2", "021-22", 42);

        sut.Name.ShouldBe("New");
        sut.City.ShouldBe("Shiraz");
        sut.Address.ShouldBe("Addr 2");
        sut.Phone.ShouldBe("021-22");
        sut.Priority.ShouldBe(42);
        sut.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void Update_LeavesCodeAndIsActiveUnchanged()
    {
        var sut = new WarehouseBuilder().WithCode("wh-01").Build();

        sut.Update("N", "C", null, null, 1);

        sut.Code.Value.ShouldBe("WH-01");
        sut.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void SetAsDefault_MarksIsDefaultAndUpdatesTimestamp()
    {
        var sut = new WarehouseBuilder().Build();

        sut.SetAsDefault();

        sut.IsDefault.ShouldBeTrue();
        sut.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void ClearDefault_UnmarksIsDefaultAndUpdatesTimestamp()
    {
        var sut = new WarehouseBuilder().AsDefault().Build();

        sut.ClearDefault();

        sut.IsDefault.ShouldBeFalse();
        sut.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void Activate_WhenAlreadyActive_IsNoOpAndLeavesUpdatedAtNull()
    {
        var sut = new WarehouseBuilder().Build();

        sut.Activate();

        sut.IsActive.ShouldBeTrue();
        sut.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public void Deactivate_WhenActive_SetsInactiveAndUpdatesTimestamp()
    {
        var sut = new WarehouseBuilder().Build();

        sut.Deactivate();

        sut.IsActive.ShouldBeFalse();
        sut.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_IsNoOp()
    {
        var sut = new WarehouseBuilder().Build();
        sut.Deactivate();
        var updatedAtBefore = sut.UpdatedAt;

        sut.Deactivate();

        sut.IsActive.ShouldBeFalse();
        sut.UpdatedAt.ShouldBe(updatedAtBefore);
    }

    [Fact]
    public void Activate_AfterDeactivate_RestoresActiveAndUpdatesTimestamp()
    {
        var sut = new WarehouseBuilder().Build();
        sut.Deactivate();

        sut.Activate();

        sut.IsActive.ShouldBeTrue();
        sut.UpdatedAt.ShouldNotBeNull();
    }
}
