using Application.Auth.EventHandlers;
using Application.Common.Events;
using Domain.User.Events;
using Domain.User.ValueObjects;

namespace Tests.Application.Auth.EventHandlers;

public class UserPhoneChangedEventHandlerTests
{
    private readonly ICacheInvalidationService _cacheInvalidation = Substitute.For<ICacheInvalidationService>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly ILogger<UserPhoneChangedEventHandler> _logger = Substitute.For<ILogger<UserPhoneChangedEventHandler>>();
    private readonly UserPhoneChangedEventHandler _sut;

    public UserPhoneChangedEventHandlerTests()
    {
        _sut = new UserPhoneChangedEventHandler(_cacheInvalidation, _auditService, _logger);
    }

    private static UserPhoneChangedEvent BuildEvent(UserId? userId = null, string oldPhone = "09121234567", string newPhone = "09129876543")
        => new(userId ?? UserId.NewId(), PhoneNumber.Create(oldPhone), PhoneNumber.Create(newPhone));

    [Fact]
    public async Task Handle_WithValidEvent_InvalidatesUserCacheOnce()
    {
        var userId = UserId.NewId();
        var notification = new DomainEventNotification<UserPhoneChangedEvent>(BuildEvent(userId));

        await _sut.Handle(notification, CancellationToken.None);

        await _cacheInvalidation.Received(1).InvalidateUserCacheAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidEvent_LogsSystemEventWithUserIdAndBothPhoneNumbers()
    {
        var userId = UserId.NewId();
        const string oldPhone = "09121111111";
        const string newPhone = "09122222222";
        var notification = new DomainEventNotification<UserPhoneChangedEvent>(BuildEvent(userId, oldPhone, newPhone));

        await _sut.Handle(notification, CancellationToken.None);

        await _auditService.Received(1).LogSystemEventAsync(
            "User phone changed",
            Arg.Is<string>(s =>
                s!.Contains(userId.Value.ToString())
                && s.Contains(oldPhone)
                && s.Contains(newPhone)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidatesCacheBeforeLoggingAudit()
    {
        var userId = UserId.NewId();
        var notification = new DomainEventNotification<UserPhoneChangedEvent>(BuildEvent(userId));

        await _sut.Handle(notification, CancellationToken.None);

        Received.InOrder(() =>
        {
            _cacheInvalidation.InvalidateUserCacheAsync(userId, Arg.Any<CancellationToken>());
            _auditService.LogSystemEventAsync(
                "User phone changed",
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenToBothDependencies()
    {
        using var cts = new CancellationTokenSource();
        var userId = UserId.NewId();
        var notification = new DomainEventNotification<UserPhoneChangedEvent>(BuildEvent(userId));

        await _sut.Handle(notification, cts.Token);

        await _cacheInvalidation.Received(1).InvalidateUserCacheAsync(userId, cts.Token);
        await _auditService.Received(1).LogSystemEventAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            cts.Token);
    }

    [Fact]
    public async Task Handle_InvokesEachDependencyExactlyOnce()
    {
        var notification = new DomainEventNotification<UserPhoneChangedEvent>(BuildEvent());

        await _sut.Handle(notification, CancellationToken.None);

        _cacheInvalidation.ReceivedCalls().Count().ShouldBe(1);
        _auditService.ReceivedCalls().Count().ShouldBe(1);
    }

    [Theory]
    [InlineData("09121234567", "09359876543")]
    [InlineData("09301112233", "09124445566")]
    [InlineData("09197778899", "09011234567")]
    public async Task Handle_WithVariousPhoneCombinations_LogsBothInAuditDetails(string oldPhone, string newPhone)
    {
        var notification = new DomainEventNotification<UserPhoneChangedEvent>(BuildEvent(null, oldPhone, newPhone));

        await _sut.Handle(notification, CancellationToken.None);

        await _auditService.Received(1).LogSystemEventAsync(
            "User phone changed",
            Arg.Is<string>(s => s!.Contains(oldPhone) && s.Contains(newPhone)),
            Arg.Any<CancellationToken>());
    }
}
