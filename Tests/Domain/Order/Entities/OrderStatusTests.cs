using Domain.Order.Entities;
using Domain.Order.Events;
using SharedKernel.Abstractions.Interfaces;
using SharedKernel.Exceptions;

namespace Tests.Domain.Order.Entities;

public class OrderStatusTests
{
    [Fact]
    public void Create_WithValidInput_ReturnsInitializedOrderStatus()
    {
        var sut = OrderStatus.Create(
            name: "Shipped",
            displayName: "ارسال شده",
            icon: "truck",
            color: "#00AA00",
            sortOrder: 5,
            allowCancel: false,
            allowEdit: false);

        sut.ShouldNotBeNull();
        sut.Id.ShouldNotBeNull();
        sut.Id.Value.ShouldNotBe(Guid.Empty);
        sut.Name.ShouldBe("Shipped");
        sut.DisplayName.ShouldBe("ارسال شده");
        sut.Icon.ShouldBe("truck");
        sut.Color.ShouldBe("#00AA00");
        sut.SortOrder.ShouldBe(5);
        sut.IsActive.ShouldBeTrue();
        sut.IsDefault.ShouldBeFalse();
        sut.AllowCancel.ShouldBeFalse();
        sut.AllowEdit.ShouldBeFalse();
        sut.RowVersion.ShouldNotBeNull();
        sut.RowVersion.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Create_TrimsAllStringInputs()
    {
        var sut = OrderStatus.Create("  Pending  ", "  در انتظار  ", "  clock  ", "  #FFAA00  ", 1);

        sut.Name.ShouldBe("Pending");
        sut.DisplayName.ShouldBe("در انتظار");
        sut.Icon.ShouldBe("clock");
        sut.Color.ShouldBe("#FFAA00");
    }

    [Fact]
    public void Create_WithNullOptionalIconAndColor_LeavesThemNull()
    {
        var sut = OrderStatus.Create("Pending", "در انتظار");

        sut.Icon.ShouldBeNull();
        sut.Color.ShouldBeNull();
        sut.SortOrder.ShouldBe(0);
        sut.AllowCancel.ShouldBeFalse();
        sut.AllowEdit.ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespaceName_ThrowsArgumentException(string? name)
    {
        Should.Throw<ArgumentException>(() => OrderStatus.Create(name!, "Display"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespaceDisplayName_ThrowsArgumentException(string? displayName)
    {
        Should.Throw<ArgumentException>(() => OrderStatus.Create("Name", displayName!));
    }

    [Fact]
    public void Create_RaisesExactlyOneOrderStatusCreatedDomainEvent()
    {
        var sut = OrderStatus.Create("Pending", "در انتظار", sortOrder: 3);

        sut.DomainEvents.Count.ShouldBe(1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<OrderStatusCreatedDomainEvent>();
        evt.OrderStatusId.ShouldBe(sut.Id);
        evt.Name.ShouldBe("Pending");
        evt.DisplayName.ShouldBe("در انتظار");
        evt.SortOrder.ShouldBe(3);
    }

    [Fact]
    public void Create_ImplementsIActivatable()
    {
        var sut = OrderStatus.Create("Pending", "در انتظار");

        sut.ShouldBeAssignableTo<IActivatable>();
    }

    [Fact]
    public void Update_WithValidInput_AppliesTrimmedFieldsAndBumpsRowVersion()
    {
        var sut = OrderStatus.Create("Pending", "در انتظار");
        var previousRowVersion = sut.RowVersion;

        sut.Update("  آماده ارسال  ", "  box  ", "  #123456  ", 9, allowCancel: true, allowEdit: true);

        sut.DisplayName.ShouldBe("آماده ارسال");
        sut.Icon.ShouldBe("box");
        sut.Color.ShouldBe("#123456");
        sut.SortOrder.ShouldBe(9);
        sut.AllowCancel.ShouldBeTrue();
        sut.AllowEdit.ShouldBeTrue();
        sut.RowVersion.ShouldNotBe(previousRowVersion);
    }

    [Fact]
    public void Update_DoesNotChangeName()
    {
        var sut = OrderStatus.Create("Pending", "در انتظار");

        sut.Update("در انتظار جدید", null, null, 0, false, false);

        sut.Name.ShouldBe("Pending");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithNullOrWhitespaceDisplayName_ThrowsArgumentException(string? displayName)
    {
        var sut = OrderStatus.Create("Pending", "در انتظار");

        Should.Throw<ArgumentException>(() => sut.Update(displayName!, null, null, 0, false, false));
    }

    [Fact]
    public void Update_RaisesOrderStatusUpdatedDomainEvent()
    {
        var sut = OrderStatus.Create("Pending", "در انتظار");
        sut.ClearDomainEvents();

        sut.Update("آماده", null, null, 5, false, false);

        sut.DomainEvents.Count.ShouldBe(1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<OrderStatusUpdatedDomainEvent>();
        evt.OrderStatusId.ShouldBe(sut.Id);
        evt.DisplayName.ShouldBe("آماده");
        evt.SortOrder.ShouldBe(5);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_IsNoOpAndRaisesNoEvent()
    {
        var sut = OrderStatus.Create("Pending", "در انتظار");
        var rowVersionBefore = sut.RowVersion;
        sut.ClearDomainEvents();

        sut.Activate();

        sut.IsActive.ShouldBeTrue();
        sut.DomainEvents.ShouldBeEmpty();
        sut.RowVersion.ShouldBe(rowVersionBefore);
    }

    [Fact]
    public void Activate_WhenDeactivated_ReactivatesAndRaisesEvent()
    {
        var sut = OrderStatus.Create("Pending", "در انتظار");
        sut.Deactivate();
        sut.ClearDomainEvents();
        var rowVersionBefore = sut.RowVersion;

        sut.Activate();

        sut.IsActive.ShouldBeTrue();
        sut.RowVersion.ShouldNotBe(rowVersionBefore);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<OrderStatusActivationChangedDomainEvent>();
        evt.OrderStatusId.ShouldBe(sut.Id);
        evt.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Deactivate_WhenActiveAndNotDefault_MarksInactiveAndRaisesEvent()
    {
        var sut = OrderStatus.Create("Pending", "در انتظار");
        sut.ClearDomainEvents();

        sut.Deactivate();

        sut.IsActive.ShouldBeFalse();
        var evt = sut.DomainEvents.Single().ShouldBeOfType<OrderStatusActivationChangedDomainEvent>();
        evt.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_IsNoOp()
    {
        var sut = OrderStatus.Create("Pending", "در انتظار");
        sut.Deactivate();
        sut.ClearDomainEvents();

        sut.Deactivate();

        sut.IsActive.ShouldBeFalse();
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Deactivate_WhenDefault_ThrowsDomainException()
    {
        var sut = OrderStatus.Create("Pending", "در انتظار");
        sut.SetAsDefault();

        Should.Throw<DomainException>(() => sut.Deactivate());
        sut.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void SetAsDefault_WhenActive_MarksDefaultAndRaisesEvent()
    {
        var sut = OrderStatus.Create("Pending", "در انتظار");
        sut.ClearDomainEvents();

        sut.SetAsDefault();

        sut.IsDefault.ShouldBeTrue();
        var evt = sut.DomainEvents.Single().ShouldBeOfType<OrderStatusDefaultChangedDomainEvent>();
        evt.IsDefault.ShouldBeTrue();
    }

    [Fact]
    public void SetAsDefault_WhenAlreadyDefault_IsNoOp()
    {
        var sut = OrderStatus.Create("Pending", "در انتظار");
        sut.SetAsDefault();
        sut.ClearDomainEvents();

        sut.SetAsDefault();

        sut.IsDefault.ShouldBeTrue();
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void SetAsDefault_WhenInactive_ThrowsDomainException()
    {
        var sut = OrderStatus.Create("Pending", "در انتظار");
        sut.Deactivate();

        Should.Throw<DomainException>(() => sut.SetAsDefault());
        sut.IsDefault.ShouldBeFalse();
    }

    [Fact]
    public void UnsetAsDefault_WhenDefault_ClearsDefaultAndRaisesEvent()
    {
        var sut = OrderStatus.Create("Pending", "در انتظار");
        sut.SetAsDefault();
        sut.ClearDomainEvents();

        sut.UnsetAsDefault();

        sut.IsDefault.ShouldBeFalse();
        var evt = sut.DomainEvents.Single().ShouldBeOfType<OrderStatusDefaultChangedDomainEvent>();
        evt.IsDefault.ShouldBeFalse();
    }

    [Fact]
    public void UnsetAsDefault_WhenNotDefault_IsNoOp()
    {
        var sut = OrderStatus.Create("Pending", "در انتظار");
        sut.ClearDomainEvents();

        sut.UnsetAsDefault();

        sut.IsDefault.ShouldBeFalse();
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void MarkAsDeleted_RaisesOrderStatusDeletedDomainEvent()
    {
        var sut = OrderStatus.Create("Pending", "در انتظار");
        sut.ClearDomainEvents();

        sut.MarkAsDeleted();

        var evt = sut.DomainEvents.Single().ShouldBeOfType<OrderStatusDeletedDomainEvent>();
        evt.OrderStatusId.ShouldBe(sut.Id);
        evt.Name.ShouldBe("Pending");
    }
}
