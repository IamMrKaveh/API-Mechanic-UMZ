using Application.Wallet.Contracts;
using Application.Wallet.Features.Queries.GetAdminDebitRequests;
using Application.Wallet.Features.Shared;
using NSubstitute;
using Shouldly;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;
using Xunit;

namespace Tests.Application.Wallet.Features.Queries.GetAdminDebitRequests;

public sealed class GetAdminDebitRequestsHandlerTests
{
    private readonly IWalletDebitRequestQueryService _queryService = Substitute.For<IWalletDebitRequestQueryService>();
    private readonly GetAdminDebitRequestsHandler _sut;

    public GetAdminDebitRequestsHandlerTests()
    {
        _sut = new GetAdminDebitRequestsHandler(_queryService);
    }

    private static AdminDebitRequestListItemDto CreateItem(
        Guid? id = null,
        Guid? ownerId = null,
        decimal amount = 100_000m,
        string status = "Pending") =>
        new(
            id ?? Guid.NewGuid(),
            Guid.NewGuid(),
            ownerId ?? Guid.NewGuid(),
            "Owner Full Name",
            amount,
            "Reason",
            "Description",
            Guid.NewGuid(),
            "Admin Full Name",
            status,
            null,
            new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 4, 12, 0, 0, DateTimeKind.Utc),
            null);

    [Fact]
    public async Task Handle_WhenServiceReturnsResults_ReturnsPaginatedSuccess()
    {
        var items = new List<AdminDebitRequestListItemDto> { CreateItem(), CreateItem() };
        var paged = PaginatedResult<AdminDebitRequestListItemDto>.Create(items, totalCount: 2, page: 1, pageSize: 10);
        _queryService
            .GetPageAsync(1, 10, Arg.Any<WalletDebitRequestFilter?>(), Arg.Any<CancellationToken>())
            .Returns(paged);

        var query = new GetAdminDebitRequestsQuery(null, null, null, null, null, 1, 10);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Items.Count.ShouldBe(2);
        result.Value.TotalCount.ShouldBe(2);
        result.Value.Page.ShouldBe(1);
        result.Value.PageSize.ShouldBe(10);
    }

    [Fact]
    public async Task Handle_WhenNoResults_ReturnsEmptyPagedSuccess()
    {
        var paged = PaginatedResult<AdminDebitRequestListItemDto>.Create(
            new List<AdminDebitRequestListItemDto>(), totalCount: 0, page: 1, pageSize: 20);
        _queryService
            .GetPageAsync(1, 20, Arg.Any<WalletDebitRequestFilter?>(), Arg.Any<CancellationToken>())
            .Returns(paged);

        var query = new GetAdminDebitRequestsQuery(null, null, null, null, null, 1, 20);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Items.ShouldBeEmpty();
        result.Value.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_WhenAllFiltersProvided_MapsQueryToWalletDebitRequestFilter()
    {
        var ownerId = Guid.NewGuid();
        var requestedBy = Guid.NewGuid();
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

        WalletDebitRequestFilter? captured = null;
        _queryService
            .GetPageAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Do<WalletDebitRequestFilter?>(f => captured = f), Arg.Any<CancellationToken>())
            .Returns(PaginatedResult<AdminDebitRequestListItemDto>.Create(
                new List<AdminDebitRequestListItemDto>(), 0, 1, 20));

        var query = new GetAdminDebitRequestsQuery(ownerId, requestedBy, "Pending", from, to, 1, 20);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        captured.ShouldNotBeNull();
        captured!.OwnerId.ShouldBe(ownerId);
        captured.RequestedBy.ShouldBe(requestedBy);
        captured.Status.ShouldBe("Pending");
        captured.FromDate.ShouldBe(from);
        captured.ToDate.ShouldBe(to);
    }

    [Fact]
    public async Task Handle_WhenNoFilters_PassesNullOrEmptyFilterButStillDelegates()
    {
        _queryService
            .GetPageAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<WalletDebitRequestFilter?>(), Arg.Any<CancellationToken>())
            .Returns(PaginatedResult<AdminDebitRequestListItemDto>.Create(
                new List<AdminDebitRequestListItemDto>(), 0, 1, 20));

        var query = new GetAdminDebitRequestsQuery(null, null, null, null, null, 1, 20);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        await _queryService.Received(1).GetPageAsync(
            1, 20, Arg.Any<WalletDebitRequestFilter?>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 25)]
    [InlineData(5, 50)]
    public async Task Handle_WhenPagingProvided_PassesPageAndPageSizeToService(int page, int pageSize)
    {
        _queryService
            .GetPageAsync(page, pageSize, Arg.Any<WalletDebitRequestFilter?>(), Arg.Any<CancellationToken>())
            .Returns(PaginatedResult<AdminDebitRequestListItemDto>.Create(
                new List<AdminDebitRequestListItemDto>(), 0, page, pageSize));

        var query = new GetAdminDebitRequestsQuery(null, null, null, null, null, page, pageSize);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        await _queryService.Received(1).GetPageAsync(
            page, pageSize, Arg.Any<WalletDebitRequestFilter?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCancellationTokenProvided_PropagatesTokenToService()
    {
        using var cts = new CancellationTokenSource();
        _queryService
            .GetPageAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<WalletDebitRequestFilter?>(), Arg.Any<CancellationToken>())
            .Returns(PaginatedResult<AdminDebitRequestListItemDto>.Create(
                new List<AdminDebitRequestListItemDto>(), 0, 1, 20));

        var query = new GetAdminDebitRequestsQuery(null, null, null, null, null, 1, 20);

        await _sut.Handle(query, cts.Token);

        await _queryService.Received(1).GetPageAsync(
            1, 20, Arg.Any<WalletDebitRequestFilter?>(), cts.Token);
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsResult_ForwardsItemsInSameOrder()
    {
        var first = CreateItem(amount: 111m);
        var second = CreateItem(amount: 222m);
        var third = CreateItem(amount: 333m);
        var paged = PaginatedResult<AdminDebitRequestListItemDto>.Create(
            new List<AdminDebitRequestListItemDto> { first, second, third }, 3, 1, 10);
        _queryService
            .GetPageAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<WalletDebitRequestFilter?>(), Arg.Any<CancellationToken>())
            .Returns(paged);

        var query = new GetAdminDebitRequestsQuery(null, null, null, null, null, 1, 10);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Items[0].Amount.ShouldBe(111m);
        result.Value.Items[1].Amount.ShouldBe(222m);
        result.Value.Items[2].Amount.ShouldBe(333m);
    }
}
