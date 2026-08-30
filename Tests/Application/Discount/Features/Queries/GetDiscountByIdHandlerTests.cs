using Application.Discount.Contracts;
using Application.Discount.Features.Queries.GetDiscountById;
using Application.Discount.Features.Shared;
using Domain.Discount.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Application.Discount.Features.Queries;

public class GetDiscountByIdHandlerTests
{
    private readonly IDiscountQueryService _discountQueryService = Substitute.For<IDiscountQueryService>();
    private readonly GetDiscountByIdHandler _sut;

    public GetDiscountByIdHandlerTests()
    {
        _sut = new GetDiscountByIdHandler(_discountQueryService);
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsNull_ReturnsNotFound()
    {
        var query = new GetDiscountByIdQuery(Guid.NewGuid());

        _discountQueryService
            .GetDetailByIdAsync(Arg.Any<DiscountCodeId>(), Arg.Any<CancellationToken>())
            .Returns((DiscountCodeDetailDto?)null);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsDto_ReturnsSuccessWithDto()
    {
        var id = Guid.NewGuid();
        var expected = new DiscountCodeDetailDto
        {
            Id = id,
            Code = "SUMMER2026",
            DiscountType = "Percentage",
            DiscountValue = 10m,
            UsageCount = 3,
            IsActive = true,
            IsRedeemable = true,
            CreatedAt = DateTime.UtcNow
        };

        _discountQueryService
            .GetDetailByIdAsync(Arg.Any<DiscountCodeId>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(new GetDiscountByIdQuery(id), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldNotBeNull();
        result.Value!.Id.ShouldBe(id);
        result.Value.Code.ShouldBe("SUMMER2026");
    }

    [Fact]
    public async Task Handle_WithValidId_ConvertsGuidToDiscountCodeIdAndForwardsToService()
    {
        var id = Guid.NewGuid();
        DiscountCodeId? captured = null;

        _discountQueryService
            .GetDetailByIdAsync(
                Arg.Do<DiscountCodeId>(x => captured = x),
                Arg.Any<CancellationToken>())
            .Returns((DiscountCodeDetailDto?)null);

        await _sut.Handle(new GetDiscountByIdQuery(id), CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Value.ShouldBe(id);
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToQueryService()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _discountQueryService
            .GetDetailByIdAsync(Arg.Any<DiscountCodeId>(), Arg.Any<CancellationToken>())
            .Returns((DiscountCodeDetailDto?)null);

        await _sut.Handle(new GetDiscountByIdQuery(Guid.NewGuid()), token);

        await _discountQueryService
            .Received(1)
            .GetDetailByIdAsync(Arg.Any<DiscountCodeId>(), token);
    }

    [Fact]
    public async Task Handle_WithEmptyGuid_ThrowsDomainException()
    {
        var query = new GetDiscountByIdQuery(Guid.Empty);

        await Should.ThrowAsync<DomainException>(
            () => _sut.Handle(query, CancellationToken.None));

        await _discountQueryService
            .DidNotReceiveWithAnyArgs()
            .GetDetailByIdAsync(default!, default);
    }
}
