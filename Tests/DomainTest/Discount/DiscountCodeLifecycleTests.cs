namespace Tests.DomainTest.Discount;

public class DiscountCodeLifecycleTests
{
    [Fact]
    public void Activate_WhenInactive_ShouldSetActiveToTrue()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.Deactivate();

        discount.Activate();

        discount.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ShouldNotRaiseDomainEvent()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.ClearDomainEvents();

        discount.Activate();

        discount.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Activate_WhenDeleted_ShouldThrowDomainException()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.Delete();

        var act = () => discount.Activate();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Activate_ShouldRaiseDomainActivatedEvent()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.Deactivate();
        discount.ClearDomainEvents();

        discount.Activate();

        discount.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "DiscountActivatedEvent");
    }

    [Fact]
    public void Deactivate_WhenActive_ShouldSetActiveToFalse()
    {
        var discount = new DiscountCodeBuilder().Build();

        discount.Deactivate();

        discount.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ShouldNotRaiseDomainEvent()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.Deactivate();
        discount.ClearDomainEvents();

        discount.Deactivate();

        discount.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Deactivate_ShouldRaiseDomainDeactivatedEvent()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.ClearDomainEvents();

        discount.Deactivate();

        discount.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "DiscountDeactivatedEvent");
    }

    [Fact]
    public void Delete_ShouldMarkAsDeleted()
    {
        var discount = new DiscountCodeBuilder().Build();

        discount.Delete(deletedBy: 1);

        discount.IsDeleted.Should().BeTrue();
        discount.DeletedBy.Should().Be(1);
        discount.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Delete_ShouldDeactivateDiscount()
    {
        var discount = new DiscountCodeBuilder().Build();

        discount.Delete();

        discount.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Delete_WhenAlreadyDeleted_ShouldNotChangeState()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.Delete(deletedBy: 1);
        var deletedAt = discount.DeletedAt;

        discount.Delete(deletedBy: 2);

        discount.DeletedBy.Should().Be(1);
        discount.DeletedAt.Should().Be(deletedAt);
    }

    [Fact]
    public void Delete_ShouldRaiseDomainEvent()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.ClearDomainEvents();

        discount.Delete();

        discount.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "DiscountDeletedEvent");
    }

    [Fact]
    public void Restore_WhenDeleted_ShouldMarkAsNotDeleted()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.Delete();

        discount.Restore();

        discount.IsDeleted.Should().BeFalse();
        discount.DeletedAt.Should().BeNull();
        discount.DeletedBy.Should().BeNull();
    }

    [Fact]
    public void Restore_WhenNotDeleted_ShouldNotChangeState()
    {
        var discount = new DiscountCodeBuilder().Build();
        var originalUpdatedAt = discount.UpdatedAt;

        discount.Restore();

        discount.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void IsCurrentlyValid_ActiveAndNotExpired_ShouldReturnTrue()
    {
        var discount = new DiscountCodeBuilder().Build();

        var now = DateTime.UtcNow;

        discount.IsCurrentlyValid(now).Should().BeTrue();
    }

    [Fact]
    public void IsCurrentlyValid_WhenInactive_ShouldReturnFalse()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.Deactivate();

        var now = DateTime.UtcNow;

        discount.IsCurrentlyValid(now).Should().BeFalse();
    }

    [Fact]
    public void IsCurrentlyValid_WhenDeleted_ShouldReturnFalse()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.Delete();

        var now = DateTime.UtcNow;

        discount.IsCurrentlyValid(now).Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenPastExpiryDate_ShouldReturnTrue()
    {
        var discount = new DiscountCodeBuilder()
            .AlreadyExpired()
            .Build();

        var now = DateTime.UtcNow;

        discount.IsExpired(now).Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenNoExpiryDate_ShouldReturnFalse()
    {
        var discount = new DiscountCodeBuilder().Build();

        var now = DateTime.UtcNow;

        discount.IsExpired(now).Should().BeFalse();
    }

    [Fact]
    public void HasStarted_WhenFutureStartDate_ShouldReturnFalse()
    {
        var discount = new DiscountCodeBuilder()
            .NotYetStarted()
            .Build();

        var now = DateTime.UtcNow;

        discount.HasStarted(now).Should().BeFalse();
    }

    [Fact]
    public void HasStarted_WhenNoStartDate_ShouldReturnTrue()
    {
        var discount = new DiscountCodeBuilder().Build();

        var now = DateTime.UtcNow;

        discount.HasStarted(now).Should().BeTrue();
    }

    [Fact]
    public void ChangeCode_WhenNotUsed_ShouldUpdateCode()
    {
        var discount = new DiscountCodeBuilder().Build();

        discount.ChangeCode("NEWCODE");

        discount.Code.Value.Should().Be("NEWCODE");
    }

    [Fact]
    public void ChangeCode_WhenAlreadyUsed_ShouldThrowDomainException()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.IncrementUsage();

        var act = () => discount.ChangeCode("NEWCODE");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ChangeCode_WhenDeleted_ShouldThrowDomainException()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.Delete();

        var act = () => discount.ChangeCode("NEWCODE");

        act.Should().Throw<DomainException>();
    }
}