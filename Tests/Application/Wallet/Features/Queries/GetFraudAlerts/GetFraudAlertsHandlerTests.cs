using Application.Wallet.Contracts;
using Application.Wallet.Features.Queries.GetFraudAlerts;
using Application.Wallet.Features.Shared;
using Domain.Wallet.Enums;
using NSubstitute;
using Shouldly;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;
using Xunit;

namespace Tests.Application.Wallet.Features.Queries.GetFraudAlerts;

public sealed class GetFraudAlertsHandlerTests
{
    private readonly IWalletFraudAlertQueryService _queryService = Substitute.For<IWalletFraudAlertQueryService>();
    private readonly GetFraudAlertsHandler _sut;

    public GetFraudAlertsHandlerTests()
    {
        _sut = new GetFraudAlertsHandler(_queryService);
    }

    private static WalletFraudAlertDto CreateDto(string severity = "Medium", string status = "Open") =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "User Full Name",
            "HighAmountRule",
            severity,
            "Description",
            null,
            status,
            new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            null,
            null,
            null,
            new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));

    [Fact]
    public async Task Handle_WhenServiceReturnsResults_ReturnsPaginatedSuccess()
    {
        var items = new List<WalletFraudAlertDto> { CreateDto(), CreateDto() };
        var paged = PaginatedResult<WalletFraudAlertDto>.Create(items, 2, 1, 20);
        _queryService.GetAlertsPageAsync(
                Arg.Any<FraudAlertStatus?>(), Arg.Any<FraudAlertSeverity?>(), Arg.Any<Guid?>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(paged);

        var query = new GetFraudAlertsQuery(null, null, null, 1, 20);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Items.Count.ShouldBe(2);
        result.Value.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task Handle_WhenNoResults_ReturnsEmptyPaginatedSuccess()
    {
        var paged = PaginatedResult<WalletFraudAlertDto>.Create(new List<WalletFraudAlertDto>(), 0, 1, 20);
        _queryService.GetAlertsPageAsync(
                Arg.Any<FraudAlertStatus?>(), Arg.Any<FraudAlertSeverity?>(), Arg.Any<Guid?>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(paged);

        var query = new GetFraudAlertsQuery(null, null, null, 1, 20);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_WhenAllFiltersProvided_PassesAllArgumentsToService()
    {
        var userId = Guid.NewGuid();
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var paged = PaginatedResult<WalletFraudAlertDto>.Create(new List<WalletFraudAlertDto>(), 0, 3, 15);
        _queryService.GetAlertsPageAsync(
                Arg.Any<FraudAlertStatus?>(), Arg.Any<FraudAlertSeverity?>(), Arg.Any<Guid?>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(paged);

        var query = new GetFraudAlertsQuery(
            FraudAlertStatus.Open,
            FraudAlertSeverity.High,
            userId,
            3,
            15,
            from,
            to);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        await _queryService.Received(1).GetAlertsPageAsync(
            FraudAlertStatus.Open,
            FraudAlertSeverity.High,
            userId,
            3,
            15,
            from,
            to,
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null, null, null)]
    public async Task Handle_WhenAllFiltersNull_ForwardsNulls(
        FraudAlertStatus? status, FraudAlertSeverity? severity, Guid? userId)
    {
        _queryService.GetAlertsPageAsync(
                Arg.Any<FraudAlertStatus?>(), Arg.Any<FraudAlertSeverity?>(), Arg.Any<Guid?>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(PaginatedResult<WalletFraudAlertDto>.Create(new List<WalletFraudAlertDto>(), 0, 1, 20));

        var query = new GetFraudAlertsQuery(status, severity, userId, 1, 20);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        await _queryService.Received(1).GetAlertsPageAsync(
            null, null, null, 1, 20, null, null, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(FraudAlertSeverity.Low)]
    [InlineData(FraudAlertSeverity.Medium)]
    [InlineData(FraudAlertSeverity.High)]
    [InlineData(FraudAlertSeverity.Critical)]
    public async Task Handle_WhenSeverityFilterProvided_PropagatesEnumValue(FraudAlertSeverity severity)
    {
        _queryService.GetAlertsPageAsync(
                Arg.Any<FraudAlertStatus?>(), Arg.Any<FraudAlertSeverity?>(), Arg.Any<Guid?>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(PaginatedResult<WalletFraudAlertDto>.Create(new List<WalletFraudAlertDto>(), 0, 1, 20));

        var query = new GetFraudAlertsQuery(null, severity, null, 1, 20);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        await _queryService.Received(1).GetAlertsPageAsync(
            null, severity, null, 1, 20, null, null, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(FraudAlertStatus.Open)]
    [InlineData(FraudAlertStatus.Reviewed)]
    [InlineData(FraudAlertStatus.Dismissed)]
    public async Task Handle_WhenStatusFilterProvided_PropagatesEnumValue(FraudAlertStatus status)
    {
        _queryService.GetAlertsPageAsync(
                Arg.Any<FraudAlertStatus?>(), Arg.Any<FraudAlertSeverity?>(), Arg.Any<Guid?>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(PaginatedResult<WalletFraudAlertDto>.Create(new List<WalletFraudAlertDto>(), 0, 1, 20));

        var query = new GetFraudAlertsQuery(status, null, null, 1, 20);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        await _queryService.Received(1).GetAlertsPageAsync(
            status, null, null, 1, 20, null, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCancellationTokenProvided_PropagatesTokenToService()
    {
        using var cts = new CancellationTokenSource();
        _queryService.GetAlertsPageAsync(
                Arg.Any<FraudAlertStatus?>(), Arg.Any<FraudAlertSeverity?>(), Arg.Any<Guid?>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(PaginatedResult<WalletFraudAlertDto>.Create(new List<WalletFraudAlertDto>(), 0, 1, 20));

        var query = new GetFraudAlertsQuery(null, null, null, 1, 20);

        await _sut.Handle(query, cts.Token);

        await _queryService.Received(1).GetAlertsPageAsync(
            null, null, null, 1, 20, null, null, cts.Token);
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsResult_ForwardsResultUntouched()
    {
        var expected = PaginatedResult<WalletFraudAlertDto>.Create(
            new List<WalletFraudAlertDto> { CreateDto("Critical", "Open") }, 1, 2, 5);
        _queryService.GetAlertsPageAsync(
                Arg.Any<FraudAlertStatus?>(), Arg.Any<FraudAlertSeverity?>(), Arg.Any<Guid?>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        var query = new GetFraudAlertsQuery(null, null, null, 2, 5);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(expected);
    }
}
