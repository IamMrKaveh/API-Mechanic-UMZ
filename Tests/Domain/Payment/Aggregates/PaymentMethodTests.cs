using Domain.Payment.Aggregates;
using Domain.Payment.Events;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.Payment.Aggregates;

public class PaymentMethodTests
{
    [Fact]
    public void Create_WithValidInput_ReturnsInitializedPaymentMethod()
    {
        var name = PaymentMethodName.Create("Zarinpal");
        var code = PaymentMethodCode.Create("zarinpal");
        var fee = PaymentMethodFee.Create(500m, 1m);

        var sut = new PaymentMethodBuilder()
            .WithName(name)
            .WithCode(code)
            .WithFee(fee)
            .WithDescription(" desc ")
            .WithIconUrl(" http://a/b.png ")
            .WithSortOrder(7)
            .Build();

        sut.Id.ShouldNotBeNull();
        sut.Name.ShouldBe(name);
        sut.Code.ShouldBe(code);
        sut.Fee.ShouldBe(fee);
        sut.Description.ShouldBe("desc");
        sut.IconUrl.ShouldBe("http://a/b.png");
        sut.SortOrder.ShouldBe(7);
        sut.IsActive.ShouldBeTrue();
        sut.IsDeleted.ShouldBeFalse();
        sut.DeletedAt.ShouldBeNull();
        sut.DeletedBy.ShouldBeNull();
        sut.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public void Create_SetsCreatedAtCloseToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var sut = new PaymentMethodBuilder().Build();

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.CreatedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void Create_ProducesPaymentMethodWithVersionOne()
    {
        new PaymentMethodBuilder().Build().Version.ShouldBe(1);
    }

    [Fact]
    public void Create_RaisesExactlyOnePaymentMethodCreatedEvent()
    {
        var name = PaymentMethodName.Create("Zarinpal");
        var code = PaymentMethodCode.Create("zarinpal");

        var sut = new PaymentMethodBuilder().WithName(name).WithCode(code).Build();

        sut.DomainEvents.Count.ShouldBe(1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<PaymentMethodCreatedEvent>();
        evt.PaymentMethodId.ShouldBe(sut.Id);
        evt.Name.ShouldBe(name);
        evt.Code.ShouldBe(code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespaceIconUrl_StoresNull(string? iconUrl)
    {
        new PaymentMethodBuilder().WithIconUrl(iconUrl).Build().IconUrl.ShouldBeNull();
    }

    [Fact]
    public void Create_WithNullName_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            PaymentMethod.Create(null!, PaymentMethodCode.Create("wallet"), PaymentMethodFee.None()));
    }

    [Fact]
    public void Create_WithNullCode_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            PaymentMethod.Create(PaymentMethodName.Create("Wallet"), null!, PaymentMethodFee.None()));
    }

    [Fact]
    public void Create_WithNullFee_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            PaymentMethod.Create(PaymentMethodName.Create("Wallet"), PaymentMethodCode.Create("wallet"), null!));
    }

    [Fact]
    public void Update_WithValidInput_MutatesFieldsAndRaisesUpdatedEvent()
    {
        var sut = new PaymentMethodBuilder().Build();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        var newName = PaymentMethodName.Create("Updated Name");
        var newFee = PaymentMethodFee.Create(100m, 3m);

        sut.Update(newName, newFee, "new desc", "http://x/y.png", 42);

        sut.Name.ShouldBe(newName);
        sut.Fee.ShouldBe(newFee);
        sut.Description.ShouldBe("new desc");
        sut.IconUrl.ShouldBe("http://x/y.png");
        sut.SortOrder.ShouldBe(42);
        sut.UpdatedAt.ShouldNotBeNull();
        sut.Version.ShouldBe(versionBefore + 1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<PaymentMethodUpdatedEvent>();
        evt.PaymentMethodId.ShouldBe(sut.Id);
        evt.Name.ShouldBe(newName);
    }

    [Fact]
    public void Update_WithNullName_ThrowsArgumentNullException()
    {
        var sut = new PaymentMethodBuilder().Build();

        Should.Throw<ArgumentNullException>(() =>
            sut.Update(null!, PaymentMethodFee.None(), null, null, 0));
    }

    [Fact]
    public void Update_WithNullFee_ThrowsArgumentNullException()
    {
        var sut = new PaymentMethodBuilder().Build();

        Should.Throw<ArgumentNullException>(() =>
            sut.Update(PaymentMethodName.Create("Any"), null!, null, null, 0));
    }

    [Fact]
    public void Activate_WhenAlreadyActive_IsNoOp()
    {
        var sut = new PaymentMethodBuilder().Build();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.Activate();

        sut.IsActive.ShouldBeTrue();
        sut.Version.ShouldBe(versionBefore);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Activate_AfterDeactivate_ReActivatesAndRaisesActivatedEvent()
    {
        var sut = new PaymentMethodBuilder().Build();
        sut.Deactivate();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.Activate();

        sut.IsActive.ShouldBeTrue();
        sut.UpdatedAt.ShouldNotBeNull();
        sut.Version.ShouldBe(versionBefore + 1);
        sut.DomainEvents.Single().ShouldBeOfType<PaymentMethodActivatedEvent>()
            .PaymentMethodId.ShouldBe(sut.Id);
    }

    [Fact]
    public void Deactivate_WhenActive_TransitionsAndRaisesDeactivatedEvent()
    {
        var sut = new PaymentMethodBuilder().Build();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.Deactivate();

        sut.IsActive.ShouldBeFalse();
        sut.UpdatedAt.ShouldNotBeNull();
        sut.Version.ShouldBe(versionBefore + 1);
        sut.DomainEvents.Single().ShouldBeOfType<PaymentMethodDeactivatedEvent>()
            .PaymentMethodId.ShouldBe(sut.Id);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_IsNoOp()
    {
        var sut = new PaymentMethodBuilder().Build();
        sut.Deactivate();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.Deactivate();

        sut.IsActive.ShouldBeFalse();
        sut.Version.ShouldBe(versionBefore);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void RequestDeletion_OnActiveMethod_MarksDeletedAndDeactivatesAndRaisesDeletedEvent()
    {
        var sut = new PaymentMethodBuilder().Build();
        sut.ClearDomainEvents();
        var deletedBy = UserId.NewId();
        var versionBefore = sut.Version;

        sut.RequestDeletion(deletedBy);

        sut.IsDeleted.ShouldBeTrue();
        sut.IsActive.ShouldBeFalse();
        sut.DeletedAt.ShouldNotBeNull();
        sut.DeletedBy.ShouldBe(deletedBy.Value);
        sut.UpdatedAt.ShouldNotBeNull();
        sut.Version.ShouldBe(versionBefore + 1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<PaymentMethodDeletedEvent>();
        evt.PaymentMethodId.ShouldBe(sut.Id);
        evt.DeletedBy.ShouldBe(deletedBy);
    }

    [Fact]
    public void RequestDeletion_WithNullActor_StoresNullDeletedBy()
    {
        var sut = new PaymentMethodBuilder().Build();
        sut.ClearDomainEvents();

        sut.RequestDeletion();

        sut.IsDeleted.ShouldBeTrue();
        sut.DeletedBy.ShouldBeNull();
        sut.DomainEvents.Single().ShouldBeOfType<PaymentMethodDeletedEvent>()
            .DeletedBy.ShouldBeNull();
    }

    [Fact]
    public void RequestDeletion_WhenAlreadyDeleted_IsNoOp()
    {
        var sut = new PaymentMethodBuilder().Build();
        sut.RequestDeletion();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.RequestDeletion();

        sut.Version.ShouldBe(versionBefore);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Restore_OnDeletedMethod_ClearsDeletionAndReActivates()
    {
        var sut = new PaymentMethodBuilder().Build();
        sut.RequestDeletion(UserId.NewId());
        sut.ClearDomainEvents();

        sut.Restore();

        sut.IsDeleted.ShouldBeFalse();
        sut.DeletedAt.ShouldBeNull();
        sut.DeletedBy.ShouldBeNull();
        sut.IsActive.ShouldBeTrue();
        sut.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void Restore_WhenNotDeleted_IsNoOp()
    {
        var sut = new PaymentMethodBuilder().Build();
        var updatedAtBefore = sut.UpdatedAt;

        sut.Restore();

        sut.IsDeleted.ShouldBeFalse();
        sut.UpdatedAt.ShouldBe(updatedAtBefore);
    }

    [Fact]
    public void CalculateFee_DelegatesToFeeValueObject()
    {
        var sut = new PaymentMethodBuilder().WithFee(500m, 1m).Build();

        sut.CalculateFee(Money.Create(10_000m)).Amount.ShouldBe(600m);
    }
}
