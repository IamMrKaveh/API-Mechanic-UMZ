using Application.Order.Features.Commands.CheckoutFromCart.Interfaces;
using Domain.Discount.Aggregates;
using Domain.Discount.Interfaces;
using Domain.Discount.ValueObjects;
using Infrastructure.Order.Services;
using SharedKernel.Results;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Infrastructure.Order.Services;

public class CheckoutDiscountApplicatorServiceTests
{
    private readonly IDiscountRepository _discountRepository = Substitute.For<IDiscountRepository>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly CheckoutDiscountApplicatorService _sut;

    public CheckoutDiscountApplicatorServiceTests()
    {
        _sut = new CheckoutDiscountApplicatorService(_discountRepository, _auditService);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ApplyAsync_WhenCodeIsNullOrWhitespace_ReturnsZeroDiscountWithoutRepositoryCall(string? code)
    {
        var result = await _sut.ApplyAsync(code, Money.FromDecimal(500_000m), Guid.NewGuid(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.DiscountAmount.Amount.ShouldBe(0m);
        result.Value.DiscountCodeId.ShouldBeNull();
        await _discountRepository.DidNotReceiveWithAnyArgs().GetByCodeAsync(default!, default);
        await _auditService.DidNotReceiveWithAnyArgs().LogSystemEventAsync(default!, default!, default);
    }

    [Fact]
    public async Task ApplyAsync_WhenCodeDoesNotExist_ReturnsNotFound()
    {
        _discountRepository
            .GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((DiscountCode?)null);

        var result = await _sut.ApplyAsync("MISSING", Money.FromDecimal(500_000m), Guid.NewGuid(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task ApplyAsync_WhenDiscountIsExpired_ReturnsFailure()
    {
        var discount = new DiscountCodeBuilder()
            .WithCode("OLD10")
            .WithValue(DiscountValue.Percentage(10m))
            .WithExpiresAt(DateTime.UtcNow.AddDays(-1))
            .Build();
        _discountRepository
            .GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(discount);

        var result = await _sut.ApplyAsync("OLD10", Money.FromDecimal(500_000m), Guid.NewGuid(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
        _discountRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task ApplyAsync_WhenDiscountIsDeactivated_ReturnsFailure()
    {
        var discount = new DiscountCodeBuilder()
            .WithCode("OFF10")
            .WithValue(DiscountValue.Percentage(10m))
            .Build();
        discount.Deactivate();
        _discountRepository
            .GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(discount);

        var result = await _sut.ApplyAsync("OFF10", Money.FromDecimal(500_000m), Guid.NewGuid(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
    }

    [Fact]
    public async Task ApplyAsync_WhenDiscountIsValid_AppliesRecordsUsageAndAudits()
    {
        var discount = new DiscountCodeBuilder()
            .WithCode("SAVE10")
            .WithValue(DiscountValue.Percentage(10m))
            .Build();
        _discountRepository
            .GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(discount);

        var result = await _sut.ApplyAsync("SAVE10", Money.FromDecimal(500_000m), Guid.NewGuid(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.DiscountAmount.Amount.ShouldBe(50_000m);
        result.Value.DiscountCodeId.ShouldBe(discount.Id.Value);
        _discountRepository.Received(1).Update(discount);
        await _auditService.Received(1).LogSystemEventAsync(
            "CheckoutDiscountApplied",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyAsync_WhenUsageLimitReached_ReturnsFailure()
    {
        var discount = new DiscountCodeBuilder()
            .WithCode("ONCE")
            .WithValue(DiscountValue.Fixed(10_000m))
            .WithUsageLimit(1)
            .Build();
        discount.RecordUsage(
            global::Domain.User.ValueObjects.UserId.NewId(),
            global::Domain.Order.ValueObjects.OrderId.NewId(),
            Money.FromDecimal(10_000m));
        _discountRepository
            .GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(discount);

        var result = await _sut.ApplyAsync("ONCE", Money.FromDecimal(500_000m), Guid.NewGuid(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
    }
}
