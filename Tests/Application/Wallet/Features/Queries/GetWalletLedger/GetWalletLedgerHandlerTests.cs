using Application.Wallet.Contracts;
using Application.Wallet.Features.Queries.GetWalletLedger;
using Application.Wallet.Features.Shared;
using Domain.User.ValueObjects;
using SharedKernel.Models;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Wallet.Features.Queries.GetWalletLedger;

public class GetWalletLedgerHandlerTests
{
    private readonly IWalletQueryService _walletQueryService = Substitute.For<IWalletQueryService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetWalletLedgerHandler _sut;

    public GetWalletLedgerHandlerTests()
    {
        _sut = new GetWalletLedgerHandler(_walletQueryService, _currentUserService);
    }

    private static PaginatedResult<WalletLedgerEntryDto> EmptyPage(int page = 1, int size = 10)
        => PaginatedResult<WalletLedgerEntryDto>.Create(Array.Empty<WalletLedgerEntryDto>(), 0, page, size);

    [Fact]
    public async Task Handle_WhenCurrentUserIsNullAndRequestUserIdIsNull_ReturnsUnauthorized()
    {
        _currentUserService.UserId.Returns((Guid?)null);

        var result = await _sut.Handle(new GetWalletLedgerQuery(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Unauthorized);
        await _walletQueryService.DidNotReceiveWithAnyArgs()
            .GetLedgerPageAsync(default!, default, default, default, default, default);
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIsNullAndRequestUserIdIsEmpty_ReturnsUnauthorized()
    {
        _currentUserService.UserId.Returns((Guid?)null);

        var query = new GetWalletLedgerQuery(UserId: Guid.Empty);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Unauthorized);
    }

    [Fact]
    public async Task Handle_WhenRequestUserIdIsProvidedAndNotEmpty_UsesRequestUserIdOverCurrentUser()
    {
        var currentUserId = Guid.NewGuid();
        var requestedUserId = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)currentUserId);

        _walletQueryService
            .GetLedgerPageAsync(
                Arg.Any<UserId>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<WalletLedgerFilter?>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(EmptyPage());

        UserId? capturedUserId = null;
        _walletQueryService
            .GetLedgerPageAsync(
                Arg.Do<UserId>(u => capturedUserId = u),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<WalletLedgerFilter?>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(EmptyPage());

        var query = new GetWalletLedgerQuery(UserId: requestedUserId);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        capturedUserId.ShouldNotBeNull();
        capturedUserId!.Value.ShouldBe(requestedUserId);
    }

    [Fact]
    public async Task Handle_WhenRequestUserIdIsEmpty_FallsBackToCurrentUser()
    {
        var currentUserId = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)currentUserId);

        UserId? capturedUserId = null;
        _walletQueryService
            .GetLedgerPageAsync(
                Arg.Do<UserId>(u => capturedUserId = u),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<WalletLedgerFilter?>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(EmptyPage());

        var query = new GetWalletLedgerQuery(UserId: Guid.Empty);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        capturedUserId!.Value.ShouldBe(currentUserId);
    }

    [Fact]
    public async Task Handle_PropagatesPaginationAndFilterFieldsToQueryService()
    {
        var currentUserId = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)currentUserId);

        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var query = new GetWalletLedgerQuery(
            UserId: null,
            Page: 3,
            PageSize: 50,
            FromDate: from,
            ToDate: to,
            TransactionType: "Credit",
            MinAmount: 1_000m,
            MaxAmount: 100_000m,
            SearchTerm: "top-up",
            IncludeInactiveUsers: true);

        int capturedPage = 0;
        int capturedPageSize = 0;
        WalletLedgerFilter? capturedFilter = null;
        bool capturedIncludeInactive = false;

        _walletQueryService
            .GetLedgerPageAsync(
                Arg.Any<UserId>(),
                Arg.Do<int>(p => capturedPage = p),
                Arg.Do<int>(s => capturedPageSize = s),
                Arg.Do<WalletLedgerFilter?>(f => capturedFilter = f),
                Arg.Do<bool>(b => capturedIncludeInactive = b),
                Arg.Any<CancellationToken>())
            .Returns(EmptyPage(3, 50));

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        capturedPage.ShouldBe(3);
        capturedPageSize.ShouldBe(50);
        capturedIncludeInactive.ShouldBeTrue();
        capturedFilter.ShouldNotBeNull();
        capturedFilter!.FromDate.ShouldBe(from);
        capturedFilter.ToDate.ShouldBe(to);
        capturedFilter.TransactionType.ShouldBe("Credit");
        capturedFilter.MinAmount.ShouldBe(1_000m);
        capturedFilter.MaxAmount.ShouldBe(100_000m);
        capturedFilter.SearchTerm.ShouldBe("top-up");
    }

    [Fact]
    public async Task Handle_WhenQueryServiceReturnsPage_WrapsPageInSuccessServiceResult()
    {
        var currentUserId = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)currentUserId);

        var entries = new List<WalletLedgerEntryDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), currentUserId, 1_000m, 1_000m, "Credit", "System", Guid.Empty, "desc", DateTime.UtcNow, false),
            new(Guid.NewGuid(), Guid.NewGuid(), currentUserId, -500m, 500m, "Debit", "System", Guid.Empty, "desc-2", DateTime.UtcNow, false)
        };
        var page = PaginatedResult<WalletLedgerEntryDto>.Create(entries, 2, 1, 10);

        _walletQueryService
            .GetLedgerPageAsync(
                Arg.Any<UserId>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<WalletLedgerFilter?>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(page);

        var result = await _sut.Handle(new GetWalletLedgerQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(page);
        result.Value.Items.Count.ShouldBe(2);
        result.Value.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task Handle_DefaultsIncludeInactiveUsersToFalseWhenNotProvided()
    {
        var currentUserId = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)currentUserId);

        bool capturedIncludeInactive = true;
        _walletQueryService
            .GetLedgerPageAsync(
                Arg.Any<UserId>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<WalletLedgerFilter?>(),
                Arg.Do<bool>(b => capturedIncludeInactive = b),
                Arg.Any<CancellationToken>())
            .Returns(EmptyPage());

        var result = await _sut.Handle(new GetWalletLedgerQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        capturedIncludeInactive.ShouldBeFalse();
    }
}
