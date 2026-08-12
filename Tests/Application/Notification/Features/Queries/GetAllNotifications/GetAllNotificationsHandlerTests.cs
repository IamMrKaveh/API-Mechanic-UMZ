using Application.Notification.Contracts;
using Application.Notification.Features.Queries.GetAllNotifications;
using Application.Notification.Features.Shared;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Notification.Features.Queries.GetAllNotifications;

public class GetAllNotificationsHandlerTests
{
    private readonly INotificationQueryService _notificationQueryService = Substitute.For<INotificationQueryService>(); private readonly GetAllNotificationsHandler _sut;

    public GetAllNotificationsHandlerTests()
    {
        _sut = new GetAllNotificationsHandler(_notificationQueryService);
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsPaginatedResult_ReturnsSuccessWithSameResult()
    {
        var expected = PaginatedResult<NotificationDto>.Create(
            new List<NotificationDto>
            {
            new() { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Title = "t1", Message = "m1", Type = "SystemAlert" }
            },
            totalCount: 1,
            page: 1,
            pageSize: 20);

        _notificationQueryService
            .GetAllAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(new GetAllNotificationsQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);
    }

    [Theory]
    [InlineData(1, 20)]
    [InlineData(3, 50)]
    [InlineData(7, 10)]
    public async Task Handle_ForwardsPageAndPageSizeToNotificationQueryService(int page, int pageSize)
    {
        var payload = PaginatedResult<NotificationDto>.Create(
            new List<NotificationDto>(), totalCount: 0, page: page, pageSize: pageSize);

        _notificationQueryService
            .GetAllAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(payload);

        await _sut.Handle(new GetAllNotificationsQuery(page, pageSize), CancellationToken.None);

        await _notificationQueryService.Received(1).GetAllAsync(
            page,
            pageSize,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsEmptyPage_ReturnsSuccessWithEmptyItems()
    {
        var empty = PaginatedResult<NotificationDto>.Create(
            new List<NotificationDto>(), totalCount: 0, page: 1, pageSize: 20);

        _notificationQueryService
            .GetAllAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(empty);

        var result = await _sut.Handle(new GetAllNotificationsQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldNotBeNull();
        result.Value.Items.ShouldBeEmpty();
        result.Value.TotalCount.ShouldBe(0);
    }
}
