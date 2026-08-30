using Domain.Discount.Aggregates;
using Domain.Discount.Exceptions;
using Domain.Discount.ValueObjects;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.Discount.Entities;

public class DiscountUsageRecordTests
{
    [Fact]
    public void RecordUsage_OnRedeemableCode_ProducesInitializedUsageRecord()
    {
        var code = new DiscountCodeBuilder().WithCode("SUMMER25").Build();
        var userId = UserId.NewId();
        var orderId = OrderId.NewId();
        var discounted = Money.Create(75_000m, "IRT");

        var sut = code.RecordUsage(userId, orderId, discounted);

        sut.ShouldNotBeNull();
        sut.Id.ShouldNotBeNull();
        sut.Id.Value.ShouldNotBe(Guid.Empty);
        sut.DiscountCodeId.ShouldBe(code.Id);
        sut.Code.ShouldBe(code.Code);
        sut.UserId.ShouldBe(userId);
        sut.OrderId.ShouldBe(orderId);
        sut.DiscountedAmount.ShouldBe(75_000m);
    }

    [Fact]
    public void RecordUsage_SetsUsedAtCloseToUtcNow()
    {
        var code = new DiscountCodeBuilder().Build();
        var before = DateTime.UtcNow.AddSeconds(-1);

        var sut = code.RecordUsage(UserId.NewId(), OrderId.NewId(), Money.Create(10m, "IRT"));

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.UsedAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.UsedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void RecordUsage_CapturesUsageCountAtTimeOfRecording()
    {
        var code = new DiscountCodeBuilder().Build();

        var first = code.RecordUsage(UserId.NewId(), OrderId.NewId(), Money.Create(1m, "IRT"));
        var second = code.RecordUsage(UserId.NewId(), OrderId.NewId(), Money.Create(2m, "IRT"));
        var third = code.RecordUsage(UserId.NewId(), OrderId.NewId(), Money.Create(3m, "IRT"));

        first.UsageCountAtTime.ShouldBe(1);
        second.UsageCountAtTime.ShouldBe(2);
        third.UsageCountAtTime.ShouldBe(3);
    }

    [Fact]
    public void RecordUsage_AppendsUsageToAggregateUsages()
    {
        var code = new DiscountCodeBuilder().Build();

        var sut = code.RecordUsage(UserId.NewId(), OrderId.NewId(), Money.Create(50m, "IRT"));

        code.Usages.ShouldContain(sut);
        code.Usages.Count.ShouldBe(1);
    }

    [Fact]
    public void RecordUsage_UsesCurrentCodeStringAtTimeOfRecording()
    {
        var code = new DiscountCodeBuilder().WithCode("winter40").Build();

        var sut = code.RecordUsage(UserId.NewId(), OrderId.NewId(), Money.Create(5m, "IRT"));

        sut.Code.ShouldBe(code.Code);
        sut.Code.ShouldBe("WINTER40");
    }

    [Fact]
    public void RecordUsage_OnInactiveCode_ThrowsDiscountCodeNotRedeemableExceptionAndDoesNotAppend()
    {
        var code = new DiscountCodeBuilder().Build();
        code.Deactivate();

        Should.Throw<DiscountCodeNotRedeemableException>(
            () => code.RecordUsage(UserId.NewId(), OrderId.NewId(), Money.Create(1m, "IRT")));

        code.Usages.ShouldBeEmpty();
    }

    [Fact]
    public void RecordUsage_OnExpiredCode_ThrowsDiscountCodeNotRedeemableException()
    {
        var code = new DiscountCodeBuilder()
            .WithExpiresAt(DateTime.UtcNow.AddMinutes(-1))
            .Build();

        Should.Throw<DiscountCodeNotRedeemableException>(
            () => code.RecordUsage(UserId.NewId(), OrderId.NewId(), Money.Create(1m, "IRT")));
    }

    [Fact]
    public void RecordUsage_WhenUsageLimitReached_ThrowsDiscountCodeNotRedeemableException()
    {
        var code = new DiscountCodeBuilder().WithUsageLimit(1).Build();
        code.RecordUsage(UserId.NewId(), OrderId.NewId(), Money.Create(1m, "IRT"));

        Should.Throw<DiscountCodeNotRedeemableException>(
            () => code.RecordUsage(UserId.NewId(), OrderId.NewId(), Money.Create(1m, "IRT")));
    }

    [Fact]
    public void RecordUsage_IncrementsAggregateUsageCount()
    {
        var code = new DiscountCodeBuilder().Build();
        var countBefore = code.UsageCount;

        code.RecordUsage(UserId.NewId(), OrderId.NewId(), Money.Create(1m, "IRT"));

        code.UsageCount.ShouldBe(countBefore + 1);
    }

    [Fact]
    public void RecordUsage_ProducesRecordsWithUniqueIdentities()
    {
        var code = new DiscountCodeBuilder().Build();

        var first = code.RecordUsage(UserId.NewId(), OrderId.NewId(), Money.Create(1m, "IRT"));
        var second = code.RecordUsage(UserId.NewId(), OrderId.NewId(), Money.Create(2m, "IRT"));

        first.Id.ShouldNotBe(second.Id);
    }

    [Fact]
    public void RecordUsage_StoresProvidedUserAndOrderReferences()
    {
        var code = new DiscountCodeBuilder().Build();
        var userId = UserId.NewId();
        var orderId = OrderId.NewId();

        var sut = code.RecordUsage(userId, orderId, Money.Create(10m, "IRT"));

        sut.UserId.ShouldBe(userId);
        sut.OrderId.ShouldBe(orderId);
    }

    [Fact]
    public void RecordUsage_UsesMoneyAmountAsDiscountedAmount()
    {
        var code = new DiscountCodeBuilder().Build();

        var sut = code.RecordUsage(UserId.NewId(), OrderId.NewId(), Money.Create(123_456m, "IRT"));

        sut.DiscountedAmount.ShouldBe(123_456m);
    }
}
