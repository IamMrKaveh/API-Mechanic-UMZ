using Application.Discount.Contracts;
using Application.Discount.Features.Queries.GetDiscountInfo;
using Application.Discount.Features.Shared;
using NSubstitute;
using Shouldly;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Xunit;

namespace Tests.Application.Discount.Features.Queries.GetDiscountInfo;

public class GetDiscountInfoHandlerTests
{
    private readonly IDiscountQueryService _discountQueryService = Substitute.For<IDiscountQueryService>();
    private readonly GetDiscountInfoHandler _sut;

    public GetDiscountInfoHandlerTests()
    {
        _sut = new GetDiscountInfoHandler(_discountQueryService);
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsNull_ReturnsNotFound()
    {
        _discountQueryService
            .GetDiscountInfoByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((DiscountInfoDto?)null);

        var result = await _sut.Handle(new GetDiscountInfoQuery("NONEXISTENT"), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsDto_ReturnsSuccessWithDto()
    {
        var expected = new DiscountInfoDto
        {
            Code = "WELCOME10",
            DiscountType = "Percentage",
            DiscountValue = 10m,
            MaximumDiscountAmount = 50_000m,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRedeemable = true
        };

        _discountQueryService
            .GetDiscountInfoByCodeAsync("WELCOME10", Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(new GetDiscountInfoQuery("WELCOME10"), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);
    }

    [Fact]
    public async Task Handle_ForwardsCodeExactlyAsProvided()
    {
        string? capturedCode = null;

        _discountQueryService
            .GetDiscountInfoByCodeAsync(
                Arg.Do<string>(c => capturedCode = c),
                Arg.Any<CancellationToken>())
            .Returns((DiscountInfoDto?)null);

        await _sut.Handle(new GetDiscountInfoQuery("summer-2026"), CancellationToken.None);

        capturedCode.ShouldBe("summer-2026");
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToQueryService()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _discountQueryService
            .GetDiscountInfoByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((DiscountInfoDto?)null);

        await _sut.Handle(new GetDiscountInfoQuery("ANY"), token);

        await _discountQueryService
            .Received(1)
            .GetDiscountInfoByCodeAsync("ANY", token);
    }

    [Fact]
    public async Task Handle_WhenFailure_ContainsPersianNotFoundMessage()
    {
        _discountQueryService
            .GetDiscountInfoByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((DiscountInfoDto?)null);

        var result = await _sut.Handle(new GetDiscountInfoQuery("X"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldBe("کد تخفیف یافت نشد.");
    }
}
