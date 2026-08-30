using Application.Wallet.Contracts;
using Application.Wallet.Features.Queries.GetFraudAlertById;
using Application.Wallet.Features.Shared;
using NSubstitute;
using Shouldly;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Xunit;

namespace Tests.Application.Wallet.Features.Queries.GetFraudAlertById;

public sealed class GetFraudAlertByIdHandlerTests
{
    private readonly IWalletFraudAlertQueryService _queryService = Substitute.For<IWalletFraudAlertQueryService>();
    private readonly GetFraudAlertByIdHandler _sut;

    public GetFraudAlertByIdHandlerTests()
    {
        _sut = new GetFraudAlertByIdHandler(_queryService);
    }

    private static WalletFraudAlertDto CreateDto(Guid? id = null) =>
        new(
            id ?? Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "User Full Name",
            "HighAmountRule",
            "Medium",
            "Description",
            null,
            "Open",
            new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            null,
            null,
            null,
            new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));

    [Fact]
    public async Task Handle_WhenAlertFound_ReturnsSuccessWithDto()
    {
        var alertId = Guid.NewGuid();
        var dto = CreateDto(alertId);
        _queryService.GetByIdAsync(alertId, Arg.Any<CancellationToken>()).Returns(dto);

        var query = new GetFraudAlertByIdQuery(alertId);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(dto);
        result.Value.Id.ShouldBe(alertId);
    }

    [Fact]
    public async Task Handle_WhenAlertNotFound_ReturnsNotFoundServiceResult()
    {
        var alertId = Guid.NewGuid();
        _queryService.GetByIdAsync(alertId, Arg.Any<CancellationToken>()).Returns((WalletFraudAlertDto?)null);

        var query = new GetFraudAlertByIdQuery(alertId);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
        result.Error.Code.ShouldBe(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenCalled_PassesRequestedIdToService()
    {
        var alertId = Guid.NewGuid();
        _queryService.GetByIdAsync(alertId, Arg.Any<CancellationToken>()).Returns(CreateDto(alertId));

        var query = new GetFraudAlertByIdQuery(alertId);

        await _sut.Handle(query, CancellationToken.None);

        await _queryService.Received(1).GetByIdAsync(alertId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCancellationTokenProvided_PropagatesTokenToService()
    {
        using var cts = new CancellationTokenSource();
        var alertId = Guid.NewGuid();
        _queryService.GetByIdAsync(alertId, Arg.Any<CancellationToken>()).Returns(CreateDto(alertId));

        var query = new GetFraudAlertByIdQuery(alertId);

        await _sut.Handle(query, cts.Token);

        await _queryService.Received(1).GetByIdAsync(alertId, cts.Token);
    }

    [Fact]
    public async Task Handle_WhenAlertNotFound_DoesNotReturnValue()
    {
        _queryService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((WalletFraudAlertDto?)null);

        var query = new GetFraudAlertByIdQuery(Guid.NewGuid());

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldNotBeNullOrEmpty();
    }
}
