using Application.Common.Events;
using Application.Notification.Contracts;
using Domain.Order.ValueObjects;
using Domain.Payment.Events;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Payment.EventHandlers;

namespace Tests.Infrastructure.Payment.EventHandlers;

public class PaymentSucceededNotificationEventHandlerTests
{
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();
    private readonly PaymentSucceededNotificationEventHandler _sut;

    public PaymentSucceededNotificationEventHandlerTests()
    {
        _sut = new PaymentSucceededNotificationEventHandler(_notificationService);
    }

    private static PaymentSucceededEvent BuildEvent(UserId? userId = null, long refId = 987654321L) =>
        new(
            PaymentTransactionId.NewId(),
            OrderId.NewId(),
            refId,
            userId ?? UserId.NewId(),
            Money.Create(250_000m, "IRT"));

    private static DomainEventNotification<PaymentSucceededEvent> Wrap(PaymentSucceededEvent evt) => new(evt);

    [Fact]
    public async Task Handle_WhenValid_CallsCreateNotificationExactlyOnce()
    {
        var evt = BuildEvent();

        await _sut.Handle(Wrap(evt), CancellationToken.None);

        await _notificationService.Received(1).CreateNotificationAsync(
            Arg.Any<UserId>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<Guid?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesUserIdFromEventAsRecipient()
    {
        var userId = UserId.NewId();
        var evt = BuildEvent(userId);

        await _sut.Handle(Wrap(evt), CancellationToken.None);

        await _notificationService.Received(1).CreateNotificationAsync(
            userId,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<Guid?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UsesPersianSuccessTitle()
    {
        var evt = BuildEvent();

        await _sut.Handle(Wrap(evt), CancellationToken.None);

        await _notificationService.Received(1).CreateNotificationAsync(
            Arg.Any<UserId>(),
            "پرداخت موفق",
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<Guid?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UsesPaymentSuccessNotificationType()
    {
        var evt = BuildEvent();

        await _sut.Handle(Wrap(evt), CancellationToken.None);

        await _notificationService.Received(1).CreateNotificationAsync(
            Arg.Any<UserId>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            "PaymentSuccess",
            Arg.Any<string?>(),
            Arg.Any<Guid?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_IncludesRefIdInMessageBody()
    {
        const long refId = 424242L;
        var evt = BuildEvent(refId: refId);

        await _sut.Handle(Wrap(evt), CancellationToken.None);

        await _notificationService.Received(1).CreateNotificationAsync(
            Arg.Any<UserId>(),
            Arg.Any<string>(),
            Arg.Is<string>(m => m!.Contains(refId.ToString())),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<Guid?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MessageBodyMentionsSuccessfulPayment()
    {
        var evt = BuildEvent();

        await _sut.Handle(Wrap(evt), CancellationToken.None);

        await _notificationService.Received(1).CreateNotificationAsync(
            Arg.Any<UserId>(),
            Arg.Any<string>(),
            Arg.Is<string>(m => m!.Contains("پرداخت سفارش")),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<Guid?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToNotificationService()
    {
        var evt = BuildEvent();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        await _sut.Handle(Wrap(evt), token);

        await _notificationService.Received(1).CreateNotificationAsync(
            Arg.Any<UserId>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<Guid?>(),
            Arg.Any<string?>(),
            token);
    }

    [Fact]
    public async Task Handle_WhenNotificationServiceThrows_SwallowsExceptionAndDoesNotRethrow()
    {
        var evt = BuildEvent();

        _notificationService
            .CreateNotificationAsync(
                Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("notification service down"));

        await Should.NotThrowAsync(() => _sut.Handle(Wrap(evt), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenCalledMultipleTimes_InvokesNotificationServiceOncePerCall()
    {
        var evt1 = BuildEvent();
        var evt2 = BuildEvent();

        await _sut.Handle(Wrap(evt1), CancellationToken.None);
        await _sut.Handle(Wrap(evt2), CancellationToken.None);

        await _notificationService.Received(2).CreateNotificationAsync(
            Arg.Any<UserId>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<Guid?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }
}
