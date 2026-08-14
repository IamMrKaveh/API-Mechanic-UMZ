using Application.Common.Interfaces;
using Application.Notification.Contracts;
using Application.Notification.Features.Queries.GetNotifications;
using Application.Notification.Features.Shared;
using Domain.User.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Notification.Features.Queries.GetNotifications;

public class GetNotificationsHandlerTests
{
    private readonly INotificationQueryService _notificationQueryService = Substitute.For<INotificationQueryService>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly GetNotificationsHandler _sut;

    public GetNotificationsHandlerTests()
    {
        _sut = new GetNotificationsHandler(_notificationQueryService, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenAuthenticated_ReturnsSuccessWithServiceProvidedResult()
    {
        var currentUserId = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)currentUserId);

        var expected = PaginatedResult<NotificationDto>.Create(
            new List<NotificationDto>
            {
            new() { Id = Guid.NewGuid(), UserId = currentUserId, Title = "t", Message = "m", Type = "OrderPaid" }
            },
            totalCount: 1,
            page: 1,
            pageSize: 10);

        _notificationQueryService
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(new GetNotificationsQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);
    }

    [Fact]
    public async Task Handle_WhenAuthenticated_PassesCurrentUserIdToNotificationQueryService()
    {
        var currentUserId = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)currentUserId);
        _notificationQueryService
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(PaginatedResult<NotificationDto>.Create(new List<NotificationDto>(), 0, 1, 10));

        await _sut.Handle(new GetNotificationsQuery(), CancellationToken.None);

        await _notificationQueryService.Received(1).GetByUserIdAsync(
            Arg.Is<UserId>(u => u == UserId.From(currentUserId)),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 25)]
    [InlineData(5, 50)]
    public async Task Handle_ForwardsPageAndPageSizeToNotificationQueryService(int page, int pageSize)
    {
        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _notificationQueryService
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(PaginatedResult<NotificationDto>.Create(new List<NotificationDto>(), 0, page, pageSize));

        await _sut.Handle(new GetNotificationsQuery(Page: page, PageSize: pageSize), CancellationToken.None);

        await _notificationQueryService.Received(1).GetByUserIdAsync(
            Arg.Any<UserId>(),
            page,
            pageSize,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIdIsEmpty_ThrowsDomainException()
    {
        _currentUserService.UserId.Returns((Guid?)Guid.Empty);

        await Should.ThrowAsync<DomainException>(() =>
            _sut.Handle(new GetNotificationsQuery(), CancellationToken.None));
    }
}
