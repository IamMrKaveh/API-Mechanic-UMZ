using Application.Wallet.Contracts;
using Application.Wallet.Features.Queries.GetPendingWithdrawals;
using Application.Wallet.Features.Shared;
using Domain.Wallet.Enums;
using NSubstitute;
using Shouldly;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;
using Xunit;

namespace Tests.Application.Wallet.Features.Queries.GetPendingWithdrawals;

public sealed class GetPendingWithdrawalsHandlerTests
{
    private readonly IWalletWithdrawalQueryService _queryService = Substitute.For<IWalletWithdrawalQueryService>();
    private readonly GetPendingWithdrawalsHandler _sut;

    public GetPendingWithdrawalsHandlerTests()
    {
        _sut = new GetPendingWithdrawalsHandler(_queryService);
    }

    private static WalletWithdrawalRequestDto CreateDto(string status = "Pending") =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "User Full Name",
            100_000m,
            "IR000000000000000000000000",
            "Holder",
            null,
            status,
            null,
            null,
            new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            null,
            null,
            null,
            null);

    private static PaginatedResult<WalletWithdrawalRequestDto> EmptyPage(int page = 1, int pageSize = 20) =>
        PaginatedResult<WalletWithdrawalRequestDto>.Create(new List<WalletWithdrawalRequestDto>(), 0, page, pageSize);

    [Fact]
    public async Task Handle_WhenStatusIsNull_DefaultsToPendingFilter()
    {
        _queryService.GetByStatusAsync(
                Arg.Any<WalletWithdrawalStatus?>(), Arg.Any<int>(), Arg.Any<int>(),
                Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(EmptyPage());

        var query = new GetPendingWithdrawalsQuery(null);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        await _queryService.Received(1).GetByStatusAsync(
            WalletWithdrawalStatus.Pending,
            1,
            20,
            null,
            null,
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public async Task Handle_WhenStatusIsWhitespace_DefaultsToPendingFilter(string status)
    {
        _queryService.GetByStatusAsync(
                Arg.Any<WalletWithdrawalStatus?>(), Arg.Any<int>(), Arg.Any<int>(),
                Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(EmptyPage());

        var query = new GetPendingWithdrawalsQuery(status);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        await _queryService.Received(1).GetByStatusAsync(
            WalletWithdrawalStatus.Pending, 1, 20, null, null, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("Pending", WalletWithdrawalStatus.Pending)]
    [InlineData("Approved", WalletWithdrawalStatus.Approved)]
    [InlineData("Rejected", WalletWithdrawalStatus.Rejected)]
    [InlineData("Paid", WalletWithdrawalStatus.Paid)]
    [InlineData("Cancelled", WalletWithdrawalStatus.Cancelled)]
    public async Task Handle_WhenStatusIsValidEnumString_ParsesAndForwardsIt(string statusString, WalletWithdrawalStatus expected)
    {
        _queryService.GetByStatusAsync(
                Arg.Any<WalletWithdrawalStatus?>(), Arg.Any<int>(), Arg.Any<int>(),
                Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(EmptyPage());

        var query = new GetPendingWithdrawalsQuery(statusString);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        await _queryService.Received(1).GetByStatusAsync(
            expected, 1, 20, null, null, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("PENDING")]
    [InlineData("approved")]
    [InlineData("PaId")]
    public async Task Handle_WhenStatusIsCaseInsensitive_ParsesSuccessfully(string statusString)
    {
        _queryService.GetByStatusAsync(
                Arg.Any<WalletWithdrawalStatus?>(), Arg.Any<int>(), Arg.Any<int>(),
                Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(EmptyPage());

        var query = new GetPendingWithdrawalsQuery(statusString);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        await _queryService.Received(1).GetByStatusAsync(
            Arg.Is<WalletWithdrawalStatus?>(s => s.HasValue),
            1, 20, null, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenStatusIsInvalidString_PassesNullStatusToService()
    {
        _queryService.GetByStatusAsync(
                Arg.Any<WalletWithdrawalStatus?>(), Arg.Any<int>(), Arg.Any<int>(),
                Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(EmptyPage());

        var query = new GetPendingWithdrawalsQuery("NotARealStatus");

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        await _queryService.Received(1).GetByStatusAsync(
            (WalletWithdrawalStatus?)null, 1, 20, null, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPagingAndDatesProvided_PropagatesToService()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        _queryService.GetByStatusAsync(
                Arg.Any<WalletWithdrawalStatus?>(), Arg.Any<int>(), Arg.Any<int>(),
                Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(EmptyPage(2, 50));

        var query = new GetPendingWithdrawalsQuery("Pending", 2, 50, from, to);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        await _queryService.Received(1).GetByStatusAsync(
            WalletWithdrawalStatus.Pending, 2, 50, from, to, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsItems_ReturnsPaginatedSuccess()
    {
        var items = new List<WalletWithdrawalRequestDto> { CreateDto(), CreateDto() };
        var paged = PaginatedResult<WalletWithdrawalRequestDto>.Create(items, 2, 1, 20);
        _queryService.GetByStatusAsync(
                Arg.Any<WalletWithdrawalStatus?>(), Arg.Any<int>(), Arg.Any<int>(),
                Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(paged);

        var query = new GetPendingWithdrawalsQuery();

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(paged);
        result.Value.Items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Handle_WhenNoResults_ReturnsEmptyPagedSuccess()
    {
        _queryService.GetByStatusAsync(
                Arg.Any<WalletWithdrawalStatus?>(), Arg.Any<int>(), Arg.Any<int>(),
                Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(EmptyPage());

        var query = new GetPendingWithdrawalsQuery();

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Items.ShouldBeEmpty();
        result.Value.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_WhenCancellationTokenProvided_PropagatesTokenToService()
    {
        using var cts = new CancellationTokenSource();
        _queryService.GetByStatusAsync(
                Arg.Any<WalletWithdrawalStatus?>(), Arg.Any<int>(), Arg.Any<int>(),
                Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(EmptyPage());

        var query = new GetPendingWithdrawalsQuery();

        await _sut.Handle(query, cts.Token);

        await _queryService.Received(1).GetByStatusAsync(
            WalletWithdrawalStatus.Pending, 1, 20, null, null, cts.Token);
    }
}
