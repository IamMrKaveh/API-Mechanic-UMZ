using Application.Common.Events;
using Application.Notification.Contracts;
using Application.Wallet.EventHandlers;
using Domain.User.ValueObjects;
using Domain.Wallet.Events;
using Domain.Wallet.ValueObjects;

namespace Tests.Application.Wallet.EventHandlers;

public sealed class SendWalletCreditNotificationHandlerTests
{
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly SendWalletCreditNotificationHandler _sut;

    public SendWalletCreditNotificationHandlerTests()
    {
        _sut = new SendWalletCreditNotificationHandler(_notificationService, _auditService);
    }

    private static WalletCreditedEvent BuildCreditEvent(
        string description,
        decimal amount = 50_000m,
        decimal newBalance = 150_000m,
        string currency = "IRT")
    {
        return new WalletCreditedEvent(
            WalletId.NewId(),
            UserId.NewId(),
            Money.Create(amount, currency),
            Money.Create(newBalance, currency),
            description,
            "ref-credit-001");
    }

    private static DomainEventNotification<WalletCreditedEvent> Wrap(WalletCreditedEvent evt) => new(evt);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("regular top-up")]
    [InlineData("admin-credit lowercase")]
    [InlineData(" [ADMIN-CREDIT] leading space")]
    public async Task Handle_WhenDescriptionDoesNotStartWithAdminCreditPrefix_DoesNothing(string description)
    {
        var evt = BuildCreditEvent(description);

        await _sut.Handle(Wrap(evt), CancellationToken.None);

        await _notificationService.DidNotReceiveWithAnyArgs().CreateNotificationAsync(
            default(UserId)!, default!, default!, default!, default, default, default, default);
        await _auditService.DidNotReceiveWithAnyArgs().LogSystemEventAsync(default!, default!, default);
    }

    [Fact]
    public async Task Handle_WhenDescriptionHasAdminCreditPrefix_CreatesNotificationWithExpectedFields()
    {
        var evt = BuildCreditEvent("[ADMIN-CREDIT] compensation", amount: 120_000m, newBalance: 300_000m);

        await _sut.Handle(Wrap(evt), CancellationToken.None);

        await _notificationService.Received(1).CreateNotificationAsync(
            evt.OwnerId,
            "شارژ کیف پول توسط پشتیبانی",
            Arg.Is<string>(m => m!.Contains("120,000")
                             && m.Contains("IRT")
                             && m.Contains("300,000")),
            "WalletCredit",
            "/wallet",
            evt.WalletId.Value,
            "Wallet",
            Arg.Any<CancellationToken>());
        await _auditService.DidNotReceiveWithAnyArgs().LogSystemEventAsync(default!, default!, default);
    }

    [Fact]
    public async Task Handle_WhenNotificationServiceThrows_LogsSystemEventAndSwallows()
    {
        var evt = BuildCreditEvent("[ADMIN-CREDIT] refund");
        _notificationService
            .CreateNotificationAsync(
                Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("smtp down"));

        await Should.NotThrowAsync(async () =>
            await _sut.Handle(Wrap(evt), CancellationToken.None));

        await _auditService.Received(1).LogSystemEventAsync(
            "WalletCreditNotificationFailed",
            Arg.Is<string>(s => s!.Contains(evt.OwnerId.Value.ToString())
                             && s.Contains("smtp down")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenThrough()
    {
        using var cts = new CancellationTokenSource();
        var evt = BuildCreditEvent("[ADMIN-CREDIT] bonus");

        await _sut.Handle(Wrap(evt), cts.Token);

        await _notificationService.Received(1).CreateNotificationAsync(
            Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<string?>(), cts.Token);
    }
}
