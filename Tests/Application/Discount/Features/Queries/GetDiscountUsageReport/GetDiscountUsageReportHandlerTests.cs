using Application.Discount.Contracts;
using Application.Discount.Features.Queries.GetDiscountUsageReport;
using Application.Discount.Features.Shared;
using Domain.Discount.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Application.Discount.Features.Queries.GetDiscountUsageReport;

public class GetDiscountUsageReportHandlerTests
{
    private readonly IDiscountQueryService _discountQueryService = Substitute.For<IDiscountQueryService>();
    private readonly GetDiscountUsageReportHandler _sut;

    public GetDiscountUsageReportHandlerTests()
    {
        _sut = new GetDiscountUsageReportHandler(_discountQueryService);
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsNull_ReturnsNotFound()
    {
        _discountQueryService
            .GetUsageReportByIdAsync(Arg.Any<DiscountCodeId>(), Arg.Any<CancellationToken>())
            .Returns((DiscountUsageReportDto?)null);

        var result = await _sut.Handle(
            new GetDiscountUsageReportQuery(Guid.NewGuid()),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsReport_ReturnsSuccessWithReport()
    {
        var id = Guid.NewGuid();
        var expected = new DiscountUsageReportDto
        {
            DiscountCodeId = id,
            Code = "REP1",
            TotalUsages = 3,
            TotalDiscountedAmount = 150_000m,
            UsageLimit = 100,
            Usages = new List<DiscountUsageItemDto>
            {
                new()
                {
                    UserId = Guid.NewGuid(),
                    OrderId = Guid.NewGuid(),
                    DiscountedAmount = 50_000m,
                    UsedAt = DateTime.UtcNow
                }
            }
        };

        _discountQueryService
            .GetUsageReportByIdAsync(Arg.Any<DiscountCodeId>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(new GetDiscountUsageReportQuery(id), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);
        result.Value!.TotalUsages.ShouldBe(3);
        result.Value.Usages.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_ConvertsGuidToDiscountCodeIdAndForwardsToService()
    {
        var id = Guid.NewGuid();
        DiscountCodeId? captured = null;

        _discountQueryService
            .GetUsageReportByIdAsync(
                Arg.Do<DiscountCodeId>(x => captured = x),
                Arg.Any<CancellationToken>())
            .Returns((DiscountUsageReportDto?)null);

        await _sut.Handle(new GetDiscountUsageReportQuery(id), CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Value.ShouldBe(id);
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToQueryService()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _discountQueryService
            .GetUsageReportByIdAsync(Arg.Any<DiscountCodeId>(), Arg.Any<CancellationToken>())
            .Returns((DiscountUsageReportDto?)null);

        await _sut.Handle(new GetDiscountUsageReportQuery(Guid.NewGuid()), token);

        await _discountQueryService
            .Received(1)
            .GetUsageReportByIdAsync(Arg.Any<DiscountCodeId>(), token);
    }

    [Fact]
    public async Task Handle_WhenFailure_UsesPersianNotFoundMessage()
    {
        _discountQueryService
            .GetUsageReportByIdAsync(Arg.Any<DiscountCodeId>(), Arg.Any<CancellationToken>())
            .Returns((DiscountUsageReportDto?)null);

        var result = await _sut.Handle(
            new GetDiscountUsageReportQuery(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldBe("کد تخفیف یافت نشد.");
    }

    [Fact]
    public async Task Handle_WithEmptyGuid_ThrowsDomainException()
    {
        var query = new GetDiscountUsageReportQuery(Guid.Empty);

        await Should.ThrowAsync<DomainException>(
            () => _sut.Handle(query, CancellationToken.None));

        await _discountQueryService
            .DidNotReceiveWithAnyArgs()
            .GetUsageReportByIdAsync(default!, default);
    }
}
