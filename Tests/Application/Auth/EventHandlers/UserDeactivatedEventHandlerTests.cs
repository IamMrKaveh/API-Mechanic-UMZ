using Application.Auth.EventHandlers;
using Application.Common.Events;
using Domain.User.Events;
using Domain.User.ValueObjects;

namespace Tests.Application.Auth.EventHandlers;

public class UserDeactivatedEventHandlerTests
{
    private readonly ICacheInvalidationService _cacheInvalidation = Substitute.For<ICacheInvalidationService>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly ILogger<UserDeactivatedEventHandler> _logger = Substitute.For<ILogger<UserDeactivatedEventHandler>>();
    private readonly UserDeactivatedEventHandler _sut;

    public UserDeactivatedEventHandlerTests()
    {
        _sut = new UserDeactivatedEventHandler(_cacheInvalidation, _auditService, _logger);
    }

    [Fact]
    public async Task Handle_WithValidEvent_InvalidatesUserCacheOnce()
    {
        var userId = UserId.NewId();
        var notification = new DomainEventNotification<UserDeactivatedEvent>(new UserDeactivatedEvent(userId));

        await _sut.Handle(notification, CancellationToken.None);

        await _cacheInvalidation.Received(1).InvalidateUserCacheAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidEvent_LogsSystemEventWithUserId()
    {
        var userId = UserId.NewId();
        var notification = new DomainEventNotification<UserDeactivatedEvent>(new UserDeactivatedEvent(userId));

        await _sut.Handle(notification, CancellationToken.None);

        await _auditService.Received(1).LogSystemEventAsync(
            "Deactive User",
            Arg.Is<string>(s => s!.Contains(userId.Value.ToString())),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidatesCacheBeforeLoggingAuditEvent()
    {
        var userId = UserId.NewId();
        var notification = new DomainEventNotification<UserDeactivatedEvent>(new UserDeactivatedEvent(userId));

        await _sut.Handle(notification, CancellationToken.None);

        Received.InOrder(() =>
        {
            _cacheInvalidation.InvalidateUserCacheAsync(userId, Arg.Any<CancellationToken>());
            _auditService.LogSystemEventAsync(
                "Deactive User",
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenToBothDependencies()
    {
        using var cts = new CancellationTokenSource();
        var userId = UserId.NewId();
        var notification = new DomainEventNotification<UserDeactivatedEvent>(new UserDeactivatedEvent(userId));

        await _sut.Handle(notification, cts.Token);

        await _cacheInvalidation.Received(1).InvalidateUserCacheAsync(userId, cts.Token);
        await _auditService.Received(1).LogSystemEventAsync(Arg.Any<string>(), Arg.Any<string>(), cts.Token);
    }

    [Fact]
    public async Task Handle_InvokesEachDependencyExactlyOnce()
    {
        var userId = UserId.NewId();
        var notification = new DomainEventNotification<UserDeactivatedEvent>(new UserDeactivatedEvent(userId));

        await _sut.Handle(notification, CancellationToken.None);

        _cacheInvalidation.ReceivedCalls().Count().ShouldBe(1);
        _auditService.ReceivedCalls().Count().ShouldBe(1);
    }
}
