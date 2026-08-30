using Application.Common.Interfaces;
using Application.Discount.Contracts;
using Application.Discount.Features.Queries.ValidateDiscount;
using Application.Discount.Features.Shared;
using NSubstitute;
using Shouldly;
using SharedKernel.Results;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Assertions;
using Xunit;

namespace Tests.Application.Discount.Features.Queries.ValidateDiscount;

public class ValidateDiscountHandlerTests
{
    private readonly IDiscountQueryService _discountQueryService = Substitute.For<IDiscountQueryService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly ValidateDiscountHandler _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public ValidateDiscountHandlerTests()
    {
        _currentUserService.UserId.Returns((Guid?)_userId);
        _sut = new ValidateDiscountHandler(_discountQueryService, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenValidationSucceeds_ReturnsSuccessWithResult()
    {
        var expected = new DiscountValidationResult
        {
            DiscountCodeId = Guid.NewGuid(),
            Code = "SAVE10",
            DiscountAmount = 10_000m,
            FinalAmount = 90_000m,
            DiscountType = "Percentage",
            DiscountValue = 10m,
            IsValid = true,
            Error = null
        };

        _discountQueryService
            .ValidateDiscountAsync(
                Arg.Any<string>(),
                Arg.Any<Money>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(
            new ValidateDiscountQuery("SAVE10", 100_000m, "IRT"),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);
    }

    [Fact]
    public async Task Handle_WhenValidationFailsWithError_ReturnsFailureCarryingServiceError()
    {
        var invalid = new DiscountValidationResult
        {
            Code = "EXPIRED",
            IsValid = false,
            Error = "کد تخفیف منقضی شده است."
        };

        _discountQueryService
            .ValidateDiscountAsync(
                Arg.Any<string>(),
                Arg.Any<Money>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(invalid);

        var result = await _sut.Handle(
            new ValidateDiscountQuery("EXPIRED", 50_000m, "IRT"),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(ErrorCode.Failure);
        result.Error.Message.ShouldBe("کد تخفیف منقضی شده است.");
    }

    [Fact]
    public async Task Handle_WhenValidationFailsWithNullError_ReturnsFailureWithPersianDefault()
    {
        var invalid = new DiscountValidationResult
        {
            Code = "BAD",
            IsValid = false,
            Error = null
        };

        _discountQueryService
            .ValidateDiscountAsync(
                Arg.Any<string>(),
                Arg.Any<Money>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(invalid);

        var result = await _sut.Handle(
            new ValidateDiscountQuery("BAD", 30_000m, "IRT"),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(ErrorCode.Failure);
        result.Error.Message.ShouldBe("کد تخفیف نامعتبر است.");
    }

    [Fact]
    public async Task Handle_ForwardsCodeMoneyAndUserIdToQueryService()
    {
        string? capturedCode = null;
        Money? capturedMoney = null;
        Guid capturedUserId = Guid.Empty;

        _discountQueryService
            .ValidateDiscountAsync(
                Arg.Do<string>(c => capturedCode = c),
                Arg.Do<Money>(m => capturedMoney = m),
                Arg.Do<Guid>(u => capturedUserId = u),
                Arg.Any<CancellationToken>())
            .Returns(new DiscountValidationResult { IsValid = true });

        await _sut.Handle(
            new ValidateDiscountQuery("PROMO", 250_000m, "IRT"),
            CancellationToken.None);

        capturedCode.ShouldBe("PROMO");
        capturedMoney.ShouldNotBeNull();
        capturedMoney!.Amount.ShouldBe(250_000m);
        capturedMoney.Currency.ShouldBe("IRT");
        capturedUserId.ShouldBe(_userId);
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToQueryService()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _discountQueryService
            .ValidateDiscountAsync(
                Arg.Any<string>(),
                Arg.Any<Money>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(new DiscountValidationResult { IsValid = true });

        await _sut.Handle(new ValidateDiscountQuery("C", 10_000m, "IRT"), token);

        await _discountQueryService
            .Received(1)
            .ValidateDiscountAsync(
                Arg.Any<string>(),
                Arg.Any<Money>(),
                Arg.Any<Guid>(),
                token);
    }

    [Fact]
    public async Task Handle_WithIrrCurrency_PassesMoneyWithIrrCurrency()
    {
        Money? capturedMoney = null;

        _discountQueryService
            .ValidateDiscountAsync(
                Arg.Any<string>(),
                Arg.Do<Money>(m => capturedMoney = m),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(new DiscountValidationResult { IsValid = true });

        await _sut.Handle(
            new ValidateDiscountQuery("IRR-CODE", 1_000_000m, "IRR"),
            CancellationToken.None);

        capturedMoney.ShouldNotBeNull();
        capturedMoney!.Currency.ShouldBe("IRR");
        capturedMoney.Amount.ShouldBe(1_000_000m);
    }
}
