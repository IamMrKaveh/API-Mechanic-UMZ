using Application.Auth.EventHandlers;
using Application.Common.Events;
using Domain.User.Events;
using Domain.User.ValueObjects;

namespace Tests.Application.Auth.EventHandlers;

public class UserPhoneChangedEventHandlerTests : HandlerTestBase
{
    private readonly ILogger<UserPhoneChangedEventHandler> _logger = Substitute.For<ILogger<UserPhoneChangedEventHandler>>();
    private readonly UserPhoneChangedEventHandler _sut;

    public UserPhoneChangedEventHandlerTests()
    {
        _sut = new UserPhoneChangedEventHandler(AuditService, _logger);
    }

    private static UserPhoneChangedEvent BuildEvent(UserId? userId = null, string oldPhone = "09121234567", string newPhone = "09129876543")
        => new(userId ?? UserId.NewId(), PhoneNumber.Create(oldPhone), PhoneNumber.Create(newPhone));

    [Fact]
    public async Task Handle_WithValidEvent_LogsSystemEventWithUserIdAndBothPhoneNumbers()
    {
        var userId = UserId.NewId();
        const string oldPhone = "09121111111";
        const string newPhone = "09122222222";
        var notification = new DomainEventNotification<UserPhoneChangedEvent>(BuildEvent(userId, oldPhone, newPhone));

        await _sut.Handle(notification, CancellationToken.None);

        await AuditService.Received(1).LogSystemEventAsync(
            "User phone changed",
            Arg.Is<string>(s =>
                s!.Contains(userId.Value.ToString())
                && s.Contains(oldPhone)
                && s.Contains(newPhone)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenToAuditService()
    {
        using var cts = new CancellationTokenSource();
        var notification = new DomainEventNotification<UserPhoneChangedEvent>(BuildEvent());

        await _sut.Handle(notification, cts.Token);

        await AuditService.Received(1).LogSystemEventAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            cts.Token);
    }

    [Fact]
    public async Task Handle_LogsAuditEventExactlyOnce()
    {
        var notification = new DomainEventNotification<UserPhoneChangedEvent>(BuildEvent());

        await _sut.Handle(notification, CancellationToken.None);

        AuditService.ReceivedCalls().Count().ShouldBe(1);
    }

    [Theory]
    [InlineData("09121234567", "09359876543")]
    [InlineData("09301112233", "09124445566")]
    [InlineData("09197778899", "09011234567")]
    public async Task Handle_WithVariousPhoneCombinations_LogsBothInAuditDetails(string oldPhone, string newPhone)
    {
        var notification = new DomainEventNotification<UserPhoneChangedEvent>(BuildEvent(null, oldPhone, newPhone));

        await _sut.Handle(notification, CancellationToken.None);

        await AuditService.Received(1).LogSystemEventAsync(
            "User phone changed",
            Arg.Is<string>(s => s!.Contains(oldPhone) && s.Contains(newPhone)),
            Arg.Any<CancellationToken>());
    }
}
