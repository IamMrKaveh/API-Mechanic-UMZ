using Application.Wallet.Contracts;
using Application.Wallet.Features.Queries.GetWalletStatistics;
using Application.Wallet.Features.Shared;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Wallet.Features.Queries.GetWalletStatistics;

public class GetWalletStatisticsHandlerTests
{
    private readonly IWalletQueryService _walletQueryService = Substitute.For<IWalletQueryService>();
    private readonly GetWalletStatisticsHandler _sut;

    public GetWalletStatisticsHandlerTests()
    {
        _sut = new GetWalletStatisticsHandler(_walletQueryService);
    }

    [Fact]
    public async Task Handle_WhenQueryServiceReturnsStatistics_WrapsResultInSuccess()
    {
        var stats = new WalletStatisticsDto(
            TotalSystemBalance: 1_000_000m,
            TotalReservedBalance: 100_000m,
            TotalAvailableBalance: 900_000m,
            ActiveWalletsCount: 120,
            FrozenWalletsCount: 5,
            TotalWalletsCount: 125,
            TodayCreditVolume: 50_000m,
            TodayDebitVolume: 20_000m,
            Last7DaysTransactionCount: 400,
            PendingWithdrawalsCount: 8,
            OpenFraudAlertsCount: 2,
            GeneratedAt: DateTime.UtcNow);

        _walletQueryService.GetStatisticsAsync(Arg.Any<CancellationToken>()).Returns(stats);

        var result = await _sut.Handle(new GetWalletStatisticsQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(stats);
    }

    [Fact]
    public async Task Handle_InvokesGetStatisticsAsyncExactlyOnce()
    {
        _walletQueryService
            .GetStatisticsAsync(Arg.Any<CancellationToken>())
            .Returns(new WalletStatisticsDto(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, DateTime.UtcNow));

        var result = await _sut.Handle(new GetWalletStatisticsQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        await _walletQueryService.Received(1).GetStatisticsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenToQueryService()
    {
        using var cts = new CancellationTokenSource();
        CancellationToken capturedToken = default;
        _walletQueryService
            .GetStatisticsAsync(Arg.Do<CancellationToken>(t => capturedToken = t))
            .Returns(new WalletStatisticsDto(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, DateTime.UtcNow));

        var result = await _sut.Handle(new GetWalletStatisticsQuery(), cts.Token);

        result.ShouldBeSuccess();
        capturedToken.ShouldBe(cts.Token);
    }
}
