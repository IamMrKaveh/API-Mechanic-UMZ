using Application.Discount.Contracts;
using Application.Discount.Features.Queries.GetDiscounts;
using Application.Discount.Features.Shared;
using NSubstitute;
using Shouldly;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;
using Xunit;

namespace Tests.Application.Discount.Features.Queries.GetDiscounts;

public class GetDiscountsHandlerTests
{
    private readonly IDiscountQueryService _discountQueryService = Substitute.For<IDiscountQueryService>();
    private readonly GetDiscountsHandler _sut;

    public GetDiscountsHandlerTests()
    {
        _sut = new GetDiscountsHandler(_discountQueryService);
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsItems_ReturnsSuccessWithPaginatedResult()
    {
        var items = new List<DiscountCodeDto>
        {
            new() { Id = Guid.NewGuid(), Code = "A10", DiscountType = "Percentage", DiscountValue = 10m, IsActive = true, IsRedeemable = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Code = "B20", DiscountType = "FixedAmount", DiscountValue = 20_000m, IsActive = true, IsRedeemable = true, CreatedAt = DateTime.UtcNow }
        };

        _discountQueryService
            .GetPagedAsync(
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns((items.AsReadOnly() as IReadOnlyCollection<DiscountCodeDto>, 25));

        var query = new GetDiscountsQuery(false, false, 2, 10);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeOfType<PaginatedResult<DiscountCodeDto>>();
        result.Value.Items.Count.ShouldBe(2);
        result.Value.TotalCount.ShouldBe(25);
        result.Value.Page.ShouldBe(2);
        result.Value.PageSize.ShouldBe(10);
        result.Value.TotalPages.ShouldBe(3);
        result.Value.HasNextPage.ShouldBeTrue();
        result.Value.HasPreviousPage.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsEmpty_ReturnsSuccessWithEmptyPage()
    {
        _discountQueryService
            .GetPagedAsync(
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns((Array.Empty<DiscountCodeDto>() as IReadOnlyCollection<DiscountCodeDto>, 0));

        var result = await _sut.Handle(new GetDiscountsQuery(false, false, 1, 20), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Items.ShouldBeEmpty();
        result.Value.TotalCount.ShouldBe(0);
        result.Value.IsEmpty.ShouldBeTrue();
        result.Value.HasNextPage.ShouldBeFalse();
        result.Value.HasPreviousPage.ShouldBeFalse();
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task Handle_ForwardsFiltersExactlyToQueryService(bool includeExpired, bool includeDeleted)
    {
        bool capturedExpired = !includeExpired;
        bool capturedDeleted = !includeDeleted;

        _discountQueryService
            .GetPagedAsync(
                Arg.Do<bool>(x => capturedExpired = x),
                Arg.Do<bool>(x => capturedDeleted = x),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns((Array.Empty<DiscountCodeDto>() as IReadOnlyCollection<DiscountCodeDto>, 0));

        await _sut.Handle(new GetDiscountsQuery(includeExpired, includeDeleted, 1, 10), CancellationToken.None);

        capturedExpired.ShouldBe(includeExpired);
        capturedDeleted.ShouldBe(includeDeleted);
    }

    [Fact]
    public async Task Handle_ForwardsPageAndPageSizeExactlyToQueryService()
    {
        int capturedPage = 0;
        int capturedPageSize = 0;

        _discountQueryService
            .GetPagedAsync(
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Do<int>(x => capturedPage = x),
                Arg.Do<int>(x => capturedPageSize = x),
                Arg.Any<CancellationToken>())
            .Returns((Array.Empty<DiscountCodeDto>() as IReadOnlyCollection<DiscountCodeDto>, 0));

        await _sut.Handle(new GetDiscountsQuery(true, true, 5, 50), CancellationToken.None);

        capturedPage.ShouldBe(5);
        capturedPageSize.ShouldBe(50);
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToQueryService()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _discountQueryService
            .GetPagedAsync(
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns((Array.Empty<DiscountCodeDto>() as IReadOnlyCollection<DiscountCodeDto>, 0));

        await _sut.Handle(new GetDiscountsQuery(false, false, 1, 10), token);

        await _discountQueryService
            .Received(1)
            .GetPagedAsync(false, false, 1, 10, token);
    }

    [Fact]
    public async Task Handle_WhenTotalMatchesPageSize_ReportsSinglePageWithoutNext()
    {
        var items = new List<DiscountCodeDto>
        {
            new() { Id = Guid.NewGuid(), Code = "ONLY", DiscountType = "Percentage", DiscountValue = 5m, IsActive = true, IsRedeemable = true, CreatedAt = DateTime.UtcNow }
        };

        _discountQueryService
            .GetPagedAsync(
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns((items.AsReadOnly() as IReadOnlyCollection<DiscountCodeDto>, 1));

        var result = await _sut.Handle(new GetDiscountsQuery(false, false, 1, 10), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.TotalPages.ShouldBe(1);
        result.Value.HasNextPage.ShouldBeFalse();
        result.Value.HasPreviousPage.ShouldBeFalse();
    }
}
