using Application.Wallet.Contracts;
using Application.Wallet.Features.Queries.GetWalletTransfers;
using Application.Wallet.Features.Shared;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Wallet.Features.Queries.GetWalletTransfers;

public class GetWalletTransfersHandlerTests
{
    private readonly IWalletTransferQueryService _queryService = Substitute.For<IWalletTransferQueryService>();
    private readonly GetWalletTransfersHandler _sut;

    public GetWalletTransfersHandlerTests()
    {
        _sut = new GetWalletTransfersHandler(_queryService);
    }

    private static PaginatedResult<WalletTransferDto> EmptyPage(int page = 1, int size = 20)
        => PaginatedResult<WalletTransferDto>.Create(Array.Empty<WalletTransferDto>(), 0, page, size);

    [Fact]
    public async Task Handle_WithDefaultQuery_PassesDefaultsAndReturnsSuccess()
    {
        int capturedPage = 0;
        int capturedPageSize = 0;
        WalletTransferFilter? capturedFilter = null;

        _queryService
            .GetTransfersPageAsync(
                Arg.Do<int>(p => capturedPage = p),
                Arg.Do<int>(s => capturedPageSize = s),
                Arg.Do<WalletTransferFilter?>(f => capturedFilter = f),
                Arg.Any<CancellationToken>())
            .Returns(EmptyPage());

        var result = await _sut.Handle(new GetWalletTransfersQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        capturedPage.ShouldBe(1);
        capturedPageSize.ShouldBe(20);
        capturedFilter.ShouldNotBeNull();
        capturedFilter!.UserId.ShouldBeNull();
        capturedFilter.Status.ShouldBeNull();
        capturedFilter.FromDate.ShouldBeNull();
        capturedFilter.ToDate.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_CopiesAllFilterFieldsFromRequestIntoFilter()
    {
        var userId = Guid.NewGuid();
        var from = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);

        var query = new GetWalletTransfersQuery(
            UserId: userId,
            Status: "Completed",
            FromDate: from,
            ToDate: to,
            Page: 4,
            PageSize: 25);

        WalletTransferFilter? capturedFilter = null;
        _queryService
            .GetTransfersPageAsync(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Do<WalletTransferFilter?>(f => capturedFilter = f),
                Arg.Any<CancellationToken>())
            .Returns(EmptyPage(4, 25));

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        capturedFilter.ShouldNotBeNull();
        capturedFilter!.UserId.ShouldBe(userId);
        capturedFilter.Status.ShouldBe("Completed");
        capturedFilter.FromDate.ShouldBe(from);
        capturedFilter.ToDate.ShouldBe(to);
    }

    [Fact]
    public async Task Handle_WhenQueryServiceReturnsPage_WrapsPageInSuccessServiceResult()
    {
        var items = new List<WalletTransferDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), null, 10m, "IRT", null, "Completed", 0, DateTime.UtcNow, "c1", DateTime.UtcNow, DateTime.UtcNow, null, null),
            new(Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), null, 20m, "IRT", null, "Pending", 0, DateTime.UtcNow, "c2", DateTime.UtcNow, null, null, null)
        };
        var page = PaginatedResult<WalletTransferDto>.Create(items, 2, 1, 20);

        _queryService
            .GetTransfersPageAsync(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<WalletTransferFilter?>(),
                Arg.Any<CancellationToken>())
            .Returns(page);

        var result = await _sut.Handle(new GetWalletTransfersQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(page);
        result.Value.Items.Count.ShouldBe(2);
        result.Value.TotalCount.ShouldBe(2);
    }

    [Theory]
    [InlineData(1, 20)]
    [InlineData(2, 50)]
    [InlineData(10, 100)]
    public async Task Handle_ForwardsPageAndPageSizeExactlyAsProvided(int page, int pageSize)
    {
        int capturedPage = 0;
        int capturedPageSize = 0;
        _queryService
            .GetTransfersPageAsync(
                Arg.Do<int>(p => capturedPage = p),
                Arg.Do<int>(s => capturedPageSize = s),
                Arg.Any<WalletTransferFilter?>(),
                Arg.Any<CancellationToken>())
            .Returns(EmptyPage(page, pageSize));

        var result = await _sut.Handle(
            new GetWalletTransfersQuery(Page: page, PageSize: pageSize),
            CancellationToken.None);

        result.ShouldBeSuccess();
        capturedPage.ShouldBe(page);
        capturedPageSize.ShouldBe(pageSize);
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenToQueryService()
    {
        using var cts = new CancellationTokenSource();
        CancellationToken capturedToken = default;
        _queryService
            .GetTransfersPageAsync(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<WalletTransferFilter?>(),
                Arg.Do<CancellationToken>(t => capturedToken = t))
            .Returns(EmptyPage());

        await _sut.Handle(new GetWalletTransfersQuery(), cts.Token);

        capturedToken.ShouldBe(cts.Token);
    }
}
