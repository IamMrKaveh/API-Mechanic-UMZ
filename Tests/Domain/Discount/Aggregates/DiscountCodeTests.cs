using Domain.Discount.Events;
using Domain.Discount.Exceptions;
using Domain.Discount.ValueObjects;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Abstractions.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.Discount.Aggregates;

public class DiscountCodeTests
{
    [Fact]
    public void Create_WithValidInput_ReturnsInitializedDiscountCode()
    {
        var id = DiscountCodeId.NewId();
        var value = DiscountValue.Percentage(15m);
        var maxAmount = Money.Create(50_000m, "IRT");

        var sut = new DiscountCodeBuilder()
            .WithId(id)
            .WithCode("SAVE15")
            .WithValue(value)
            .WithMaximumDiscountAmount(maxAmount)
            .WithUsageLimit(100)
            .Build();

        sut.Id.ShouldBe(id);
        sut.Code.ShouldBe("SAVE15");
        sut.Value.ShouldBe(value);
        sut.MaximumDiscountAmount.ShouldBe(maxAmount);
        sut.UsageLimit.ShouldBe(100);
        sut.UsageCount.ShouldBe(0);
        sut.IsActive.ShouldBeTrue();
        sut.Restrictions.ShouldBeEmpty();
        sut.Usages.ShouldBeEmpty();
    }

    [Fact]
    public void Create_TrimsAndUppercasesCode()
    {
        var sut = new DiscountCodeBuilder().WithCode("  save10  ").Build();

        sut.Code.ShouldBe("SAVE10");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespaceCode_ThrowsDomainException(string? code)
    {
        Should.Throw<DomainException>(() => new DiscountCodeBuilder().WithCode(code!).Build());
    }

    [Fact]
    public void Create_WhenExpiresAtEqualsStartsAt_ThrowsInvalidDiscountException()
    {
        var t = new DateTime(2026, 6, 1);

        Should.Throw<InvalidDiscountException>(() =>
            new DiscountCodeBuilder().WithStartsAt(t).WithExpiresAt(t).Build());
    }

    [Fact]
    public void Create_WhenExpiresAtBeforeStartsAt_ThrowsInvalidDiscountException()
    {
        var starts = new DateTime(2026, 6, 1);
        var expires = new DateTime(2026, 5, 1);

        Should.Throw<InvalidDiscountException>(() =>
            new DiscountCodeBuilder().WithStartsAt(starts).WithExpiresAt(expires).Build());
    }

    [Fact]
    public void Create_WithOnlyExpiresAtInPast_Succeeds()
    {
        var pastExpiry = DateTime.UtcNow.AddDays(-1);

        var sut = new DiscountCodeBuilder().WithExpiresAt(pastExpiry).Build();

        sut.ExpiresAt.ShouldBe(pastExpiry);
        sut.IsExpired.ShouldBeTrue();
    }

    [Fact]
    public void Create_SetsCreatedAtAndUpdatedAtCloseToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var sut = new DiscountCodeBuilder().Build();

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.CreatedAt.ShouldBeLessThanOrEqualTo(after);
        sut.UpdatedAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.UpdatedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void Create_ProducesDiscountCodeWithVersionOne()
    {
        new DiscountCodeBuilder().Build().Version.ShouldBe(1);
    }

    [Fact]
    public void Create_RaisesExactlyOneDiscountCodeCreatedEvent()
    {
        var sut = new DiscountCodeBuilder().Build();

        sut.DomainEvents.Count.ShouldBe(1);
        sut.DomainEvents.Single().ShouldBeOfType<DiscountCodeCreatedEvent>();
    }

    [Fact]
    public void Create_LeavesSoftDeleteFieldsAtDefaults()
    {
        var sut = new DiscountCodeBuilder().Build();

        sut.ShouldBeAssignableTo<ISoftDeletable>();
        sut.IsDeleted.ShouldBeFalse();
        sut.DeletedAt.ShouldBeNull();
        sut.DeletedBy.ShouldBeNull();
    }

    [Fact]
    public void Create_EventCarriesRawInputCodeNotNormalized()
    {
        var sut = new DiscountCodeBuilder().WithCode("  save10  ").Build();

        var evt = sut.DomainEvents.Single().ShouldBeOfType<DiscountCodeCreatedEvent>();
        evt.Code.ShouldBe("  save10  ");
        sut.Code.ShouldBe("SAVE10");
    }

    [Fact]
    public void IsExpired_WhenExpiresAtInPast_ReturnsTrue()
    {
        var sut = new DiscountCodeBuilder().WithExpiresAt(DateTime.UtcNow.AddMinutes(-1)).Build();

        sut.IsExpired.ShouldBeTrue();
    }

    [Fact]
    public void IsExpired_WhenExpiresAtInFutureOrNull_ReturnsFalse()
    {
        new DiscountCodeBuilder().Build().IsExpired.ShouldBeFalse();
        new DiscountCodeBuilder().WithExpiresAt(DateTime.UtcNow.AddDays(30)).Build().IsExpired.ShouldBeFalse();
    }

    [Fact]
    public void HasStarted_WhenStartsAtInFuture_ReturnsFalse()
    {
        var sut = new DiscountCodeBuilder().WithStartsAt(DateTime.UtcNow.AddDays(1)).Build();

        sut.HasStarted.ShouldBeFalse();
    }

    [Fact]
    public void HasStarted_WhenStartsAtNullOrInPast_ReturnsTrue()
    {
        new DiscountCodeBuilder().Build().HasStarted.ShouldBeTrue();
        new DiscountCodeBuilder().WithStartsAt(DateTime.UtcNow.AddDays(-1)).Build().HasStarted.ShouldBeTrue();
    }

    [Fact]
    public void HasReachedUsageLimit_WhenUsageLimitNotSet_ReturnsFalse()
    {
        new DiscountCodeBuilder().WithUsageLimit(null).Build().HasReachedUsageLimit.ShouldBeFalse();
    }

    [Fact]
    public void IsRedeemable_ForFreshCodeWithoutRestrictions_ReturnsTrue()
    {
        new DiscountCodeBuilder().Build().IsRedeemable.ShouldBeTrue();
    }

    [Fact]
    public void IsRedeemable_ForExpiredCode_ReturnsFalse()
    {
        var sut = new DiscountCodeBuilder().WithExpiresAt(DateTime.UtcNow.AddMinutes(-1)).Build();

        sut.IsRedeemable.ShouldBeFalse();
    }

    [Fact]
    public void IsRedeemable_ForCodeWithFutureStart_ReturnsFalse()
    {
        var sut = new DiscountCodeBuilder().WithStartsAt(DateTime.UtcNow.AddDays(1)).Build();

        sut.IsRedeemable.ShouldBeFalse();
    }

    [Fact]
    public void IsRedeemable_ForInactiveCode_ReturnsFalse()
    {
        var sut = new DiscountCodeBuilder().Build();
        sut.Deactivate();

        sut.IsRedeemable.ShouldBeFalse();
    }

    [Fact]
    public void Update_WithValidInput_AppliesChangesIncrementsVersionAndDoesNotRaiseEvent()
    {
        var sut = new DiscountCodeBuilder().Build();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;
        var newValue = DiscountValue.Fixed(500m);

        sut.Update(newValue, Money.Create(1000m, "IRT"), 50, null, DateTime.UtcNow.AddDays(30));

        sut.Value.ShouldBe(newValue);
        sut.UsageLimit.ShouldBe(50);
        sut.Version.ShouldBe(versionBefore + 1);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Update_DoesNotChangeCodeText()
    {
        var sut = new DiscountCodeBuilder().WithCode("SAVE10").Build();

        sut.Update(DiscountValue.Percentage(20m), null, null, null, null);

        sut.Code.ShouldBe("SAVE10");
    }

    [Fact]
    public void Update_DoesNotResetUsageCount()
    {
        var sut = new DiscountCodeBuilder().WithUsageLimit(10).Build();
        sut.RecordUsage(UserId.NewId(), OrderId.NewId(), Money.Create(5m, "IRT"));
        sut.UsageCount.ShouldBe(1);

        sut.Update(DiscountValue.Percentage(20m), null, 20, null, null);

        sut.UsageCount.ShouldBe(1);
    }

    [Fact]
    public void Update_WhenExpiresAtBeforeStartsAt_ThrowsInvalidDiscountException()
    {
        var sut = new DiscountCodeBuilder().Build();
        var starts = new DateTime(2026, 6, 1);
        var expires = new DateTime(2026, 5, 1);

        Should.Throw<InvalidDiscountException>(() =>
            sut.Update(DiscountValue.Percentage(10m), null, null, starts, expires));
    }

    [Fact]
    public void ValidateForApplication_OnActiveNotStartedCode_ReturnsInvalid()
    {
        var sut = new DiscountCodeBuilder().WithStartsAt(DateTime.UtcNow.AddDays(1)).Build();

        var validation = sut.ValidateForApplication(Money.Create(100m, "IRT"));

        validation.IsValid.ShouldBeFalse();
        validation.FailureReason.ShouldContain("فعال");
    }

    [Fact]
    public void ValidateForApplication_OnExpiredCode_ReturnsInvalid()
    {
        var sut = new DiscountCodeBuilder().WithExpiresAt(DateTime.UtcNow.AddDays(-1)).Build();

        var validation = sut.ValidateForApplication(Money.Create(100m, "IRT"));

        validation.IsValid.ShouldBeFalse();
        validation.FailureReason.ShouldContain("منقضی");
    }

    [Fact]
    public void ValidateForApplication_OnInactiveCode_ReturnsInvalid()
    {
        var sut = new DiscountCodeBuilder().Build();
        sut.Deactivate();

        var validation = sut.ValidateForApplication(Money.Create(100m, "IRT"));

        validation.IsValid.ShouldBeFalse();
        validation.FailureReason.ShouldContain("غیرفعال");
    }

    [Fact]
    public void ValidateForApplication_OnUsageLimitReached_ReturnsInvalid()
    {
        var sut = new DiscountCodeBuilder().WithUsageLimit(1).Build();
        sut.RecordUsage(UserId.NewId(), OrderId.NewId(), Money.Create(5m, "IRT"));

        var validation = sut.ValidateForApplication(Money.Create(100m, "IRT"));

        validation.IsValid.ShouldBeFalse();
        validation.FailureReason.ShouldContain("سقف");
    }

    [Fact]
    public void ValidateForApplication_OnRedeemableCode_ReturnsValid()
    {
        var sut = new DiscountCodeBuilder().Build();

        var validation = sut.ValidateForApplication(Money.Create(100m, "IRT"));

        validation.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(20, 100, 20)]
    [InlineData(50, 200, 100)]
    [InlineData(100, 100, 100)]
    public void CalculateDiscount_ForPercentageWithoutCap_ReturnsPercentageOfOrder(
        decimal percent, decimal orderAmount, decimal expected)
    {
        var sut = new DiscountCodeBuilder()
            .WithValue(DiscountValue.Percentage(percent))
            .Build();

        sut.CalculateDiscount(Money.Create(orderAmount, "IRT")).Amount.ShouldBe(expected);
    }

    [Fact]
    public void CalculateDiscount_ForFixedWithinOrder_ReturnsFixedAmount()
    {
        var sut = new DiscountCodeBuilder().WithValue(DiscountValue.Fixed(30m)).Build();

        sut.CalculateDiscount(Money.Create(100m, "IRT")).Amount.ShouldBe(30m);
    }

    [Fact]
    public void CalculateDiscount_ForFixedExceedingOrder_ReturnsOrderAmount()
    {
        var sut = new DiscountCodeBuilder().WithValue(DiscountValue.Fixed(500m)).Build();

        sut.CalculateDiscount(Money.Create(100m, "IRT")).Amount.ShouldBe(100m);
    }

    [Fact]
    public void CalculateDiscount_ForFreeShipping_ReturnsZero()
    {
        var sut = new DiscountCodeBuilder().WithValue(DiscountValue.FreeShipping()).Build();

        sut.CalculateDiscount(Money.Create(100m, "IRT")).Amount.ShouldBe(0m);
    }

    [Fact]
    public void CalculateDiscount_WhenPercentageExceedsMaximumCap_CapsAtMaximum()
    {
        var sut = new DiscountCodeBuilder()
            .WithValue(DiscountValue.Percentage(50m))
            .WithMaximumDiscountAmount(20m, "IRT")
            .Build();

        sut.CalculateDiscount(Money.Create(100m, "IRT")).Amount.ShouldBe(20m);
    }

    [Fact]
    public void CalculateDiscount_WhenPercentageBelowMaximumCap_DoesNotCap()
    {
        var sut = new DiscountCodeBuilder()
            .WithValue(DiscountValue.Percentage(10m))
            .WithMaximumDiscountAmount(50m, "IRT")
            .Build();

        sut.CalculateDiscount(Money.Create(100m, "IRT")).Amount.ShouldBe(10m);
    }

    [Fact]
    public void RecordUsage_OnRedeemableCode_IncrementsCounterAndAppendsUsageAndRaisesEvent()
    {
        var sut = new DiscountCodeBuilder().WithUsageLimit(10).Build();
        sut.ClearDomainEvents();
        var userId = UserId.NewId();
        var orderId = OrderId.NewId();

        var usage = sut.RecordUsage(userId, orderId, Money.Create(25m, "IRT"));

        sut.UsageCount.ShouldBe(1);
        sut.Usages.Count.ShouldBe(1);
        sut.Usages.Single().ShouldBe(usage);
        usage.UserId.ShouldBe(userId);
        usage.OrderId.ShouldBe(orderId);
        usage.DiscountedAmount.ShouldBe(25m);
        usage.UsageCountAtTime.ShouldBe(1);
        usage.Code.ShouldBe(sut.Code);
        sut.DomainEvents.Count.ShouldBe(1);
        sut.DomainEvents.Single().ShouldBeOfType<DiscountCodeAppliedEvent>();
    }

    [Fact]
    public void RecordUsage_OnRedeemableCode_IncrementsVersionByTwo()
    {
        var sut = new DiscountCodeBuilder().WithUsageLimit(10).Build();
        var versionBefore = sut.Version;

        sut.RecordUsage(UserId.NewId(), OrderId.NewId(), Money.Create(5m, "IRT"));

        sut.Version.ShouldBe(versionBefore + 2);
    }

    [Fact]
    public void RecordUsage_OnInactiveCode_ThrowsDiscountCodeNotRedeemableException()
    {
        var sut = new DiscountCodeBuilder().Build();
        sut.Deactivate();

        Should.Throw<DiscountCodeNotRedeemableException>(() =>
            sut.RecordUsage(UserId.NewId(), OrderId.NewId(), Money.Create(5m, "IRT")));
    }

    [Fact]
    public void RecordUsage_OnExpiredCode_ThrowsDiscountCodeNotRedeemableException()
    {
        var sut = new DiscountCodeBuilder().WithExpiresAt(DateTime.UtcNow.AddMinutes(-1)).Build();

        Should.Throw<DiscountCodeNotRedeemableException>(() =>
            sut.RecordUsage(UserId.NewId(), OrderId.NewId(), Money.Create(5m, "IRT")));
    }

    [Fact]
    public void RecordUsage_OnCodeWithFutureStart_ThrowsDiscountCodeNotRedeemableException()
    {
        var sut = new DiscountCodeBuilder().WithStartsAt(DateTime.UtcNow.AddDays(1)).Build();

        Should.Throw<DiscountCodeNotRedeemableException>(() =>
            sut.RecordUsage(UserId.NewId(), OrderId.NewId(), Money.Create(5m, "IRT")));
    }

    [Fact]
    public void RecordUsage_WhenLimitReached_SubsequentCallThrows()
    {
        var sut = new DiscountCodeBuilder().WithUsageLimit(1).Build();
        sut.RecordUsage(UserId.NewId(), OrderId.NewId(), Money.Create(5m, "IRT"));

        Should.Throw<DiscountCodeNotRedeemableException>(() =>
            sut.RecordUsage(UserId.NewId(), OrderId.NewId(), Money.Create(5m, "IRT")));
    }

    [Fact]
    public void RecordUsage_SetsUsedAtCloseToUtcNow()
    {
        var sut = new DiscountCodeBuilder().Build();
        var before = DateTime.UtcNow.AddSeconds(-1);

        var usage = sut.RecordUsage(UserId.NewId(), OrderId.NewId(), Money.Create(5m, "IRT"));

        var after = DateTime.UtcNow.AddSeconds(1);
        usage.UsedAt.ShouldBeGreaterThanOrEqualTo(before);
        usage.UsedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_IsNoOp()
    {
        var sut = new DiscountCodeBuilder().Build();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.Activate();

        sut.IsActive.ShouldBeTrue();
        sut.Version.ShouldBe(versionBefore);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Activate_OnDeactivatedCode_SetsFlagAndRaisesEventAndBumpsVersionByTwo()
    {
        var sut = new DiscountCodeBuilder().Build();
        sut.Deactivate();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.Activate();

        sut.IsActive.ShouldBeTrue();
        sut.Version.ShouldBe(versionBefore + 2);
        sut.DomainEvents.Single().ShouldBeOfType<DiscountCodeActivatedEvent>();
    }

    [Fact]
    public void Deactivate_WhenActive_SetsFlagAndRaisesEventAndBumpsVersionByTwo()
    {
        var sut = new DiscountCodeBuilder().Build();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.Deactivate();

        sut.IsActive.ShouldBeFalse();
        sut.Version.ShouldBe(versionBefore + 2);
        sut.DomainEvents.Single().ShouldBeOfType<DiscountCodeDeactivatedEvent>();
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_IsNoOp()
    {
        var sut = new DiscountCodeBuilder().Build();
        sut.Deactivate();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.Deactivate();

        sut.Version.ShouldBe(versionBefore);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void LifecycleSequence_CreateDeactivateActivateRecordUsage_AccumulatesEventsInOrder()
    {
        var sut = new DiscountCodeBuilder().WithUsageLimit(10).Build();

        sut.Deactivate();
        sut.Activate();
        sut.RecordUsage(UserId.NewId(), OrderId.NewId(), Money.Create(5m, "IRT"));

        sut.DomainEvents.Count.ShouldBe(4);
        sut.DomainEvents.ElementAt(0).ShouldBeOfType<DiscountCodeCreatedEvent>();
        sut.DomainEvents.ElementAt(1).ShouldBeOfType<DiscountCodeDeactivatedEvent>();
        sut.DomainEvents.ElementAt(2).ShouldBeOfType<DiscountCodeActivatedEvent>();
        sut.DomainEvents.ElementAt(3).ShouldBeOfType<DiscountCodeAppliedEvent>();
    }

    [Fact]
    public void Equality_TwoDiscountCodesWithSameId_AreConsideredEqualByEntitySemantics()
    {
        var sut = new DiscountCodeBuilder().Build();

        sut.Equals(sut).ShouldBeTrue();
    }

    [Fact]
    public void Equality_TwoDiscountCodesWithDifferentIds_AreConsideredUnequal()
    {
        var a = new DiscountCodeBuilder().Build();
        var b = new DiscountCodeBuilder().Build();

        a.Equals(b).ShouldBeFalse();
    }
}
