using Application.Common.Interfaces;
using Application.Notification.Contracts;
using Application.Notification.Features.Queries.GetUnreadNotificationCount;
using Domain.User.ValueObjects;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Notification.Features.Queries.GetUnreadNotificationCount;

public class GetUnreadNotificationCountHandlerTests
{
    private readonly INotificationQueryService _notificationQueryService = Substitute.For<INotificationQueryService>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly GetUnreadNotificationCountHandler _sut;

    public GetUnreadNotificationCountHandlerTests()
    {
        _sut = new GetUnreadNotificationCountHandler(_notificationQueryService, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ReturnsSuccessWithZeroAndDoesNotCallQueryService()
    {
        _currentUserService.IsAuthenticated.Returns(false);
        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());

        var result = await _sut.Handle(new GetUnreadNotificationCountQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(0);
        await _notificationQueryService.DidNotReceiveWithAnyArgs()
            .GetUnreadCountAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenAuthenticatedButUserIdIsNull_ReturnsSuccessWithZeroAndDoesNotCallQueryService()
    {
        _currentUserService.IsAuthenticated.Returns(true);
        _currentUserService.UserId.Returns((Guid?)null);

        var result = await _sut.Handle(new GetUnreadNotificationCountQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(0);
        await _notificationQueryService.DidNotReceiveWithAnyArgs()
            .GetUnreadCountAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenAuthenticatedAndUserIdPresent_ReturnsSuccessWithServiceProvidedCount()
    {
        var currentUserId = Guid.NewGuid();
        _currentUserService.IsAuthenticated.Returns(true);
        _currentUserService.UserId.Returns((Guid?)currentUserId);
        _notificationQueryService
            .GetUnreadCountAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(7);

        var result = await _sut.Handle(new GetUnreadNotificationCountQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(7);
    }

    [Fact]
    public async Task Handle_WhenAuthenticatedAndUserIdPresent_PassesCurrentUserIdToNotificationQueryService()
    {
        var currentUserId = Guid.NewGuid();
        _currentUserService.IsAuthenticated.Returns(true);
        _currentUserService.UserId.Returns((Guid?)currentUserId);
        _notificationQueryService
            .GetUnreadCountAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(0);

        await _sut.Handle(new GetUnreadNotificationCountQuery(), CancellationToken.None);

        await _notificationQueryService.Received(1).GetUnreadCountAsync(
            Arg.Is<UserId>(u => u == UserId.From(currentUserId)),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(int.MaxValue)]
    public async Task Handle_WhenAuthenticated_ReturnsSuccessWithCountReportedByQueryService(int count)
    {
        _currentUserService.IsAuthenticated.Returns(true);
        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _notificationQueryService
            .GetUnreadCountAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(count);

        var result = await _sut.Handle(new GetUnreadNotificationCountQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(count);
    }
}
