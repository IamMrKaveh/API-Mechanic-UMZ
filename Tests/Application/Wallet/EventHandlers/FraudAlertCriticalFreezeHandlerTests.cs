using Application.Common.Events;
using Application.Wallet.EventHandlers;
using Application.Wallet.Features.Commands.FreezeWallet;
using Domain.User.ValueObjects;
using Domain.Wallet.Enums;
using Domain.Wallet.Events;
using Domain.Wallet.ValueObjects;

namespace Tests.Application.Wallet.EventHandlers;

public class FraudAlertCriticalFreezeHandlerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly FraudAlertCriticalFreezeHandler _sut;

    public FraudAlertCriticalFreezeHandlerTests()
    {
        _sut = new FraudAlertCriticalFreezeHandler(_mediator, _auditService);
    }

    private static WalletFraudAlertRaisedEvent BuildEvent(FraudAlertSeverity severity) => new(
        WalletFraudAlertId.NewId(),
        WalletId.NewId(),
        UserId.NewId(),
        "HighAmountRule",
        severity,
        "برداشت سنگین در بازه‌ی زمانی کوتاه",
        DateTime.UtcNow);

    [Theory]
    [InlineData(FraudAlertSeverity.Low)]
    [InlineData(FraudAlertSeverity.Medium)]
    [InlineData(FraudAlertSeverity.High)]
    public async Task Handle_WhenSeverityIsNotCritical_DoesNotDispatchOrAudit(FraudAlertSeverity severity)
    {
        var notification = new DomainEventNotification<WalletFraudAlertRaisedEvent>(BuildEvent(severity));

        await _sut.Handle(notification, CancellationToken.None);

        await _mediator.DidNotReceive().Send(Arg.Any<FreezeWalletCommand>(), Arg.Any<CancellationToken>());
        await _auditService.DidNotReceiveWithAnyArgs().LogSystemEventAsync(default!, default!, default);
    }

    [Fact]
    public async Task Handle_WhenSeverityCriticalAndFreezeSucceeds_LogsSuccessAudit()
    {
        _mediator
            .Send(Arg.Any<FreezeWalletCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Success(Unit.Value));

        var notification = new DomainEventNotification<WalletFraudAlertRaisedEvent>(
            BuildEvent(FraudAlertSeverity.Critical));

        await _sut.Handle(notification, CancellationToken.None);

        await _mediator.Received(1).Send(Arg.Any<FreezeWalletCommand>(), Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogSystemEventAsync(
            "FraudAutoFreezeApplied",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSeverityCriticalAndFreezeFails_LogsFailureAudit()
    {
        _mediator
            .Send(Arg.Any<FreezeWalletCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Failure(Error.Conflict("wallet already frozen")));

        var notification = new DomainEventNotification<WalletFraudAlertRaisedEvent>(
            BuildEvent(FraudAlertSeverity.Critical));

        await _sut.Handle(notification, CancellationToken.None);

        await _auditService.Received(1).LogSystemEventAsync(
            "FraudAutoFreezeFailed",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await _auditService.DidNotReceive().LogSystemEventAsync(
            "FraudAutoFreezeApplied",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenMediatorThrows_LogsErrorAuditAndDoesNotPropagate()
    {
        _mediator
            .Send(Arg.Any<FreezeWalletCommand>(), Arg.Any<CancellationToken>())
            .Returns<ServiceResult<Unit>>(_ => throw new InvalidOperationException("boom"));

        var notification = new DomainEventNotification<WalletFraudAlertRaisedEvent>(
            BuildEvent(FraudAlertSeverity.Critical));

        await Should.NotThrowAsync(() => _sut.Handle(notification, CancellationToken.None));

        await _auditService.Received(1).LogSystemEventAsync(
            "FraudAutoFreezeError",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSeverityCritical_ForwardsUserIdAndRuleNameToFreezeCommand()
    {
        var evt = BuildEvent(FraudAlertSeverity.Critical);
        _mediator
            .Send(Arg.Any<FreezeWalletCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Success(Unit.Value));

        await _sut.Handle(new DomainEventNotification<WalletFraudAlertRaisedEvent>(evt), CancellationToken.None);

        await _mediator.Received(1).Send(
            Arg.Is<FreezeWalletCommand>(c =>
                c!.UserId == evt.UserId.Value &&
                c.Reason.Contains(evt.RuleName) &&
                c.Reason.Contains(evt.Description)),
            Arg.Any<CancellationToken>());
    }
}
