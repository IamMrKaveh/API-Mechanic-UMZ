using Application.Wallet.Contracts;
using Application.Wallet.Features.Queries.GetMyWithdrawals;
using Application.Wallet.Features.Shared;
using Domain.User.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.Models;

namespace Tests.Application.Wallet.Features.Queries.GetMyWithdrawals;

public sealed class GetMyWithdrawalsHandlerTests
{
    private readonly IWalletWithdrawalQueryService _queryService = Substitute.For<IWalletWithdrawalQueryService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetMyWithdrawalsHandler _sut;

    public GetMyWithdrawalsHandlerTests()
    {
        _sut = new GetMyWithdrawalsHandler(_queryService, _currentUserService);
    }

    private static WalletWithdrawalRequestDto CreateDto(decimal amount = 100_000m, string status = "Pending") =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "User Full Name",
            amount,
            "IR000000000000000000000000",
            "Account Holder",
            "desc",
            status,
            null,
            null,
            new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            null,
            null,
            null,
            null);

    [Fact]
    public async Task Handle_WhenServiceReturnsResults_ReturnsPaginatedSuccess()
    {
        var userId = UserId.NewId();
        _currentUserService.UserId.Returns(userId.Value);
        var items = new List<WalletWithdrawalRequestDto> { CreateDto(), CreateDto() };
        var paged = PaginatedResult<WalletWithdrawalRequestDto>.Create(items, 2, 1, 10);
        _queryService.GetByUserAsync(Arg.Any<UserId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(paged);

        var query = new GetMyWithdrawalsQuery();

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Items.Count.ShouldBe(2);
        result.Value.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task Handle_WhenCalled_PassesCurrentUserIdToService()
    {
        var userId = UserId.NewId();
        _currentUserService.UserId.Returns(userId.Value);
        _queryService.GetByUserAsync(Arg.Any<UserId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(PaginatedResult<WalletWithdrawalRequestDto>.Create(new List<WalletWithdrawalRequestDto>(), 0, 1, 10));

        var query = new GetMyWithdrawalsQuery();

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        await _queryService.Received(1).GetByUserAsync(
            Arg.Is<UserId>(u => u == userId),
            1,
            10,
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 25)]
    [InlineData(3, 50)]
    public async Task Handle_WhenPagingProvided_PropagatesPageAndPageSize(int page, int pageSize)
    {
        var userId = UserId.NewId();
        _currentUserService.UserId.Returns(userId.Value);
        _queryService.GetByUserAsync(Arg.Any<UserId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(PaginatedResult<WalletWithdrawalRequestDto>.Create(new List<WalletWithdrawalRequestDto>(), 0, page, pageSize));

        var query = new GetMyWithdrawalsQuery(page, pageSize);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        await _queryService.Received(1).GetByUserAsync(
            Arg.Any<UserId>(), page, pageSize, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoResults_ReturnsEmptyPage()
    {
        var userId = UserId.NewId();
        _currentUserService.UserId.Returns(userId.Value);
        _queryService.GetByUserAsync(Arg.Any<UserId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(PaginatedResult<WalletWithdrawalRequestDto>.Create(new List<WalletWithdrawalRequestDto>(), 0, 1, 10));

        var query = new GetMyWithdrawalsQuery();

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Items.ShouldBeEmpty();
        result.Value.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_WhenCancellationTokenProvided_PropagatesTokenToService()
    {
        using var cts = new CancellationTokenSource();
        var userId = UserId.NewId();
        _currentUserService.UserId.Returns(userId.Value);
        _queryService.GetByUserAsync(Arg.Any<UserId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(PaginatedResult<WalletWithdrawalRequestDto>.Create(new List<WalletWithdrawalRequestDto>(), 0, 1, 10));

        var query = new GetMyWithdrawalsQuery();

        await _sut.Handle(query, cts.Token);

        await _queryService.Received(1).GetByUserAsync(
            Arg.Any<UserId>(), 1, 10, cts.Token);
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIdIsEmpty_ThrowsDomainException()
    {
        _currentUserService.UserId.Returns(Guid.Empty);

        var query = new GetMyWithdrawalsQuery();

        var act = async () => await _sut.Handle(query, CancellationToken.None);

        await act.ShouldThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsResult_ForwardsResultUntouched()
    {
        var userId = UserId.NewId();
        _currentUserService.UserId.Returns(userId.Value);
        var expected = PaginatedResult<WalletWithdrawalRequestDto>.Create(
            new List<WalletWithdrawalRequestDto> { CreateDto(500_000m, "Approved") }, 1, 1, 10);
        _queryService.GetByUserAsync(Arg.Any<UserId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var query = new GetMyWithdrawalsQuery();

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(expected);
    }
}
