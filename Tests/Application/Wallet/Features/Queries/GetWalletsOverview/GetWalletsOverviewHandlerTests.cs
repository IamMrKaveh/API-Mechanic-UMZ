using Application.Wallet.Contracts;
using Application.Wallet.Features.Queries.GetWalletsOverview;
using Application.Wallet.Features.Shared;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Wallet.Features.Queries.GetWalletsOverview;

public class GetWalletsOverviewHandlerTests
{
    private readonly IWalletQueryService _walletQueryService = Substitute.For<IWalletQueryService>();
    private readonly GetWalletsOverviewHandler _sut;

    public GetWalletsOverviewHandlerTests()
    {
        _sut = new GetWalletsOverviewHandler(_walletQueryService);
    }

    private static PaginatedResult<WalletOverviewDto> EmptyPage(int page = 1, int size = 20)
        => PaginatedResult<WalletOverviewDto>.Create(Array.Empty<WalletOverviewDto>(), 0, page, size);

    [Fact]
    public async Task Handle_WithDefaultQuery_PassesDefaultsAndReturnsSuccess()
    {
        int capturedPage = 0;
        int capturedPageSize = 0;
        bool capturedIncludeInactive = false;
        WalletOverviewFilter? capturedFilter = null;

        _walletQueryService
            .GetOverviewPageAsync(
                Arg.Do<int>(p => capturedPage = p),
                Arg.Do<int>(s => capturedPageSize = s),
                Arg.Do<WalletOverviewFilter?>(f => capturedFilter = f),
                Arg.Do<bool>(b => capturedIncludeInactive = b),
                Arg.Any<CancellationToken>())
            .Returns(EmptyPage());

        var result = await _sut.Handle(new GetWalletsOverviewQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        capturedPage.ShouldBe(1);
        capturedPageSize.ShouldBe(20);
        capturedIncludeInactive.ShouldBeTrue();
        capturedFilter.ShouldNotBeNull();
        capturedFilter!.Search.ShouldBeNull();
        capturedFilter.IsFrozen.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_CopiesAllFilterFieldsFromRequestIntoFilter()
    {
        var createdFrom = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var createdTo = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var query = new GetWalletsOverviewQuery(
            Search: "ali",
            IsFrozen: true,
            MinBalance: 100m,
            MaxBalance: 1_000_000m,
            CreatedFrom: createdFrom,
            CreatedTo: createdTo,
            SortBy: "balance_desc",
            Page: 2,
            PageSize: 50);

        WalletOverviewFilter? capturedFilter = null;
        _walletQueryService
            .GetOverviewPageAsync(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Do<WalletOverviewFilter?>(f => capturedFilter = f),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(EmptyPage(2, 50));

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        capturedFilter.ShouldNotBeNull();
        capturedFilter!.Search.ShouldBe("ali");
        capturedFilter.IsFrozen.ShouldBe(true);
        capturedFilter.MinBalance.ShouldBe(100m);
        capturedFilter.MaxBalance.ShouldBe(1_000_000m);
        capturedFilter.CreatedFrom.ShouldBe(createdFrom);
        capturedFilter.CreatedTo.ShouldBe(createdTo);
        capturedFilter.SortBy.ShouldBe("balance_desc");
    }

    [Fact]
    public async Task Handle_AlwaysCallsQueryServiceWithIncludeInactiveUsersTrue()
    {
        bool capturedIncludeInactive = false;
        _walletQueryService
            .GetOverviewPageAsync(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<WalletOverviewFilter?>(),
                Arg.Do<bool>(b => capturedIncludeInactive = b),
                Arg.Any<CancellationToken>())
            .Returns(EmptyPage());

        var result = await _sut.Handle(new GetWalletsOverviewQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        capturedIncludeInactive.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenQueryServiceReturnsPage_WrapsPageInSuccessServiceResult()
    {
        var items = new List<WalletOverviewDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Ali", "ali@example.com", 500m, 0m, 500m, true, null, DateTime.UtcNow, DateTime.UtcNow),
            new(Guid.NewGuid(), Guid.NewGuid(), "Sara", "sara@example.com", 800m, 100m, 700m, false, "risk", DateTime.UtcNow, DateTime.UtcNow)
        };
        var page = PaginatedResult<WalletOverviewDto>.Create(items, 2, 1, 20);

        _walletQueryService
            .GetOverviewPageAsync(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<WalletOverviewFilter?>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(page);

        var result = await _sut.Handle(new GetWalletsOverviewQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(page);
        result.Value.Items.Count.ShouldBe(2);
    }

    [Theory]
    [InlineData(1, 20)]
    [InlineData(5, 100)]
    [InlineData(10, 200)]
    public async Task Handle_ForwardsPageAndPageSizeExactlyAsProvided(int page, int pageSize)
    {
        int capturedPage = 0;
        int capturedPageSize = 0;
        _walletQueryService
            .GetOverviewPageAsync(
                Arg.Do<int>(p => capturedPage = p),
                Arg.Do<int>(s => capturedPageSize = s),
                Arg.Any<WalletOverviewFilter?>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(EmptyPage(page, pageSize));

        var result = await _sut.Handle(
            new GetWalletsOverviewQuery(Page: page, PageSize: pageSize),
            CancellationToken.None);

        result.ShouldBeSuccess();
        capturedPage.ShouldBe(page);
        capturedPageSize.ShouldBe(pageSize);
    }
}
