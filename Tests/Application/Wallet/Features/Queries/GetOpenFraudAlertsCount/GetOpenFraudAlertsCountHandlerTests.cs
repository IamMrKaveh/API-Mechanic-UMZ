using Application.Wallet.Contracts;
using Application.Wallet.Features.Queries.GetOpenFraudAlertsCount;
using NSubstitute;
using Shouldly;
using Tests.TestInfrastructure.Assertions;
using Xunit;

namespace Tests.Application.Wallet.Features.Queries.GetOpenFraudAlertsCount;

public sealed class GetOpenFraudAlertsCountHandlerTests
{
    private readonly IWalletFraudAlertQueryService _queryService = Substitute.For<IWalletFraudAlertQueryService>();
    private readonly GetOpenFraudAlertsCountHandler _sut;

    public GetOpenFraudAlertsCountHandlerTests()
    {
        _sut = new GetOpenFraudAlertsCountHandler(_queryService);
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsPositiveCount_ReturnsSuccessWithThatCount()
    {
        _queryService.GetOpenAlertsCountAsync(Arg.Any<CancellationToken>()).Returns(7);

        var result = await _sut.Handle(new GetOpenFraudAlertsCountQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(7);
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsZero_ReturnsSuccessWithZero()
    {
        _queryService.GetOpenAlertsCountAsync(Arg.Any<CancellationToken>()).Returns(0);

        var result = await _sut.Handle(new GetOpenFraudAlertsCountQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_WhenCalled_DelegatesToService()
    {
        _queryService.GetOpenAlertsCountAsync(Arg.Any<CancellationToken>()).Returns(3);

        await _sut.Handle(new GetOpenFraudAlertsCountQuery(), CancellationToken.None);

        await _queryService.Received(1).GetOpenAlertsCountAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCancellationTokenProvided_PropagatesTokenToService()
    {
        using var cts = new CancellationTokenSource();
        _queryService.GetOpenAlertsCountAsync(Arg.Any<CancellationToken>()).Returns(0);

        await _sut.Handle(new GetOpenFraudAlertsCountQuery(), cts.Token);

        await _queryService.Received(1).GetOpenAlertsCountAsync(cts.Token);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(int.MaxValue)]
    public async Task Handle_WhenServiceReturnsVariousCounts_ReturnsExactValue(int count)
    {
        _queryService.GetOpenAlertsCountAsync(Arg.Any<CancellationToken>()).Returns(count);

        var result = await _sut.Handle(new GetOpenFraudAlertsCountQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(count);
    }
}
