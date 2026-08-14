using Application.Notification.Contracts;
using Application.Notification.Features.Commands.AdminSendNotification;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Notification.Features.Commands.AdminSendNotification;

public class AdminSendNotificationHandlerTests
{
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>(); private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>(); private readonly AdminSendNotificationHandler _sut;

    public AdminSendNotificationHandlerTests()
    {
        _sut = new AdminSendNotificationHandler(_notificationService, _userRepository);
    }

    private static AdminSendNotificationCommand ToSingleUser(Guid userId) =>
        new(
            Title: "عنوان",
            Message: "متن",
            Type: "SystemAlert",
            ActionUrl: "/dashboard",
            SendToAll: false,
            UserId: userId);

    private static AdminSendNotificationCommand ToAll() =>
        new(
            Title: "عنوان همگانی",
            Message: "متن همگانی",
            Type: "SystemAlert",
            ActionUrl: null,
            SendToAll: true,
            UserId: null);

    [Fact]
    public async Task Handle_WhenSendToAllTrue_LoadsActiveUserIdsFromRepository()
    {
        _userRepository
            .GetAllActiveUserIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { Guid.NewGuid() });

        await _sut.Handle(ToAll(), CancellationToken.None);

        await _userRepository.Received(1).GetAllActiveUserIdsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSendToAllTrue_CreatesOneNotificationPerActiveUser()
    {
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var user3 = Guid.NewGuid();
        _userRepository
            .GetAllActiveUserIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { user1, user2, user3 });

        var result = await _sut.Handle(ToAll(), CancellationToken.None);

        result.ShouldBeSuccess();
        await _notificationService.Received(3).CreateNotificationAsync(
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
    public async Task Handle_WhenSendToAllTrue_ForwardsTitleMessageTypeAndActionUrlToNotificationService()
    {
        var userId = Guid.NewGuid();
        _userRepository
            .GetAllActiveUserIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { userId });

        var command = new AdminSendNotificationCommand(
            Title: "عنوان-همگانی",
            Message: "متن-همگانی",
            Type: "OrderCreated",
            ActionUrl: "/all",
            SendToAll: true,
            UserId: null);

        await _sut.Handle(command, CancellationToken.None);

        await _notificationService.Received(1).CreateNotificationAsync(
            Arg.Is<UserId>(u => u == UserId.From(userId)),
            "عنوان-همگانی",
            "متن-همگانی",
            "OrderCreated",
            "/all",
            Arg.Any<Guid?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSendToAllTrueAndNoActiveUsers_ReturnsSuccessWithoutCallingNotificationService()
    {
        _userRepository
            .GetAllActiveUserIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Guid>());

        var result = await _sut.Handle(ToAll(), CancellationToken.None);

        result.ShouldBeSuccess();
        await _notificationService.DidNotReceiveWithAnyArgs().CreateNotificationAsync(
            default!, default!, default!, default!, default, default, default, default);
    }

    [Fact]
    public async Task Handle_WhenSendToAllFalseAndUserIdIsNull_ReturnsFailureAndDoesNotCallNotificationService()
    {
        var command = new AdminSendNotificationCommand(
            Title: "عنوان",
            Message: "متن",
            Type: "SystemAlert",
            ActionUrl: null,
            SendToAll: false,
            UserId: null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
        result.ShouldFailWithType(ErrorType.Failure);
        result.Error.Message.ShouldBe("شناسه کاربر الزامی است.");

        await _notificationService.DidNotReceiveWithAnyArgs().CreateNotificationAsync(
            default!, default!, default!, default!, default, default, default, default);
        await _userRepository.DidNotReceive().GetAllActiveUserIdsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSendToAllFalseAndUserIdProvided_CreatesSingleNotificationForThatUser()
    {
        var targetUserId = Guid.NewGuid();

        var result = await _sut.Handle(ToSingleUser(targetUserId), CancellationToken.None);

        result.ShouldBeSuccess();
        await _notificationService.Received(1).CreateNotificationAsync(
            Arg.Is<UserId>(u => u == UserId.From(targetUserId)),
            "عنوان",
            "متن",
            "SystemAlert",
            "/dashboard",
            Arg.Any<Guid?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSendToAllFalseAndUserIdProvided_DoesNotLoadActiveUsers()
    {
        await _sut.Handle(ToSingleUser(Guid.NewGuid()), CancellationToken.None);

        await _userRepository.DidNotReceive().GetAllActiveUserIdsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSendToAllFalseAndUserIdIsEmptyGuid_ThrowsDomainException()
    {
        var command = ToSingleUser(Guid.Empty);

        await Should.ThrowAsync<DomainException>(() =>
            _sut.Handle(command, CancellationToken.None));
    }
}
