using Application.Audit.Contracts;
using Domain.Discount.Aggregates;
using Domain.Discount.Interfaces;
using Domain.Discount.ValueObjects;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Discount.Services;
using SharedKernel.Results;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Infrastructure.Discount.Services;

public class DiscountServiceTests
{
    private readonly IDiscountRepository _discountRepository = Substitute.For<IDiscountRepository>(); private readonly IAuditService _auditService = Substitute.For<IAuditService>(); private readonly DiscountService _sut;

    public DiscountServiceTests()
    {
        _sut = new DiscountService(_discountRepository, _auditService);
    }

    [Fact]
    public async Task ApplyDiscountAsync_WhenCodeDoesNotExist_ReturnsFailure()
    {
        _discountRepository
            .GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((DiscountCode?)null);

        var result = await _sut.ApplyDiscountAsync(
            "MISSING",
            Money.Create(1000m, "IRT"),
            UserId.NewId(),
            OrderId.NewId(),
            CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Failure);
        await _auditService.DidNotReceive().LogOrderEventAsync(
            Arg.Any<OrderId>(),
            Arg.Any<string>(),
            Arg.Any<IpAddress>(),
            Arg.Any<UserId?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyDiscountAsync_WhenCodeIsExpired_ReturnsFailure()
    {
        var discount = new DiscountCodeBuilder()
            .WithCode("EXPIRED10")
            .WithExpiresAt(DateTime.UtcNow.AddMinutes(-1))
            .Build();

        _discountRepository
            .GetByCodeAsync("EXPIRED10", Arg.Any<CancellationToken>())
            .Returns(discount);

        var result = await _sut.ApplyDiscountAsync(
            "EXPIRED10",
            Money.Create(1000m, "IRT"),
            UserId.NewId(),
            OrderId.NewId(),
            CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Failure);
        discount.UsageCount.ShouldBe(0);
        _discountRepository.DidNotReceive().Update(Arg.Any<DiscountCode>());
        await _auditService.DidNotReceive().LogOrderEventAsync(
            Arg.Any<OrderId>(),
            Arg.Any<string>(),
            Arg.Any<IpAddress>(),
            Arg.Any<UserId?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyDiscountAsync_WhenCodeIsInactive_ReturnsFailure()
    {
        var discount = new DiscountCodeBuilder().WithCode("OFF10").Build();
        discount.Deactivate();

        _discountRepository
            .GetByCodeAsync("OFF10", Arg.Any<CancellationToken>())
            .Returns(discount);

        var result = await _sut.ApplyDiscountAsync(
            "OFF10",
            Money.Create(1000m, "IRT"),
            UserId.NewId(),
            OrderId.NewId(),
            CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Failure);
        discount.UsageCount.ShouldBe(0);
        _discountRepository.DidNotReceive().Update(Arg.Any<DiscountCode>());
    }

    [Fact]
    public async Task ApplyDiscountAsync_OnRedeemableCode_ReturnsSuccessAndRecordsUsageAndUpdatesAndLogsAudit()
    {
        var discount = new DiscountCodeBuilder()
            .WithCode("SAVE10")
            .WithValue(DiscountValue.Percentage(10m))
            .WithUsageLimit(5)
            .Build();

        var userId = UserId.NewId();
        var orderId = OrderId.NewId();

        _discountRepository
            .GetByCodeAsync("SAVE10", Arg.Any<CancellationToken>())
            .Returns(discount);

        var result = await _sut.ApplyDiscountAsync(
            "SAVE10",
            Money.Create(1000m, "IRT"),
            userId,
            orderId,
            CancellationToken.None);

        result.ShouldBeSuccess();
        discount.UsageCount.ShouldBe(1);
        discount.Usages.Count.ShouldBe(1);
        _discountRepository.Received(1).Update(discount);
        await _auditService.Received(1).LogOrderEventAsync(
            orderId,
            "DiscountApplied",
            Arg.Any<IpAddress>(),
            userId,
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(10, 1000, 100)]
    [InlineData(25, 400, 100)]
    [InlineData(50, 200, 100)]
    public async Task ApplyDiscountAsync_OnPercentageDiscount_RecordsExpectedDiscountedAmount(
        decimal percent,
        decimal orderAmount,
        decimal expectedDiscountedAmount)
    {
        var discount = new DiscountCodeBuilder()
            .WithCode("PCT")
            .WithValue(DiscountValue.Percentage(percent))
            .Build();

        _discountRepository
            .GetByCodeAsync("PCT", Arg.Any<CancellationToken>())
            .Returns(discount);

        var result = await _sut.ApplyDiscountAsync(
            "PCT",
            Money.Create(orderAmount, "IRT"),
            UserId.NewId(),
            OrderId.NewId(),
            CancellationToken.None);

        result.ShouldBeSuccess();
        discount.Usages.Single().DiscountedAmount.ShouldBe(expectedDiscountedAmount);
    }

    [Fact]
    public async Task ApplyDiscountAsync_OnFixedDiscountExceedingOrder_CapsAtOrderAmount()
    {
        var discount = new DiscountCodeBuilder()
            .WithCode("FIXED")
            .WithValue(DiscountValue.Fixed(500m))
            .Build();

        _discountRepository
            .GetByCodeAsync("FIXED", Arg.Any<CancellationToken>())
            .Returns(discount);

        var result = await _sut.ApplyDiscountAsync(
            "FIXED",
            Money.Create(100m, "IRT"),
            UserId.NewId(),
            OrderId.NewId(),
            CancellationToken.None);

        result.ShouldBeSuccess();
        discount.Usages.Single().DiscountedAmount.ShouldBe(100m);
    }

    [Fact]
    public async Task ApplyDiscountAsync_WithMaximumDiscountCap_CapsDiscountAtMaximum()
    {
        var discount = new DiscountCodeBuilder()
            .WithCode("CAPPED")
            .WithValue(DiscountValue.Percentage(50m))
            .WithMaximumDiscountAmount(20m, "IRT")
            .Build();

        _discountRepository
            .GetByCodeAsync("CAPPED", Arg.Any<CancellationToken>())
            .Returns(discount);

        var result = await _sut.ApplyDiscountAsync(
            "CAPPED",
            Money.Create(1000m, "IRT"),
            UserId.NewId(),
            OrderId.NewId(),
            CancellationToken.None);

        result.ShouldBeSuccess();
        discount.Usages.Single().DiscountedAmount.ShouldBe(20m);
    }
}
