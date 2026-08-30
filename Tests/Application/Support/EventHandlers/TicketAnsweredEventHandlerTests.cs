using Application.Common.Events;
using Application.Notification.Contracts;
using Application.Support.EventHandlers;
using Domain.Support.Aggregates;
using Domain.Support.Events;
using Domain.Support.Interfaces;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;

namespace Tests.Application.Support.EventHandlers;

public class TicketAnsweredEventHandlerTests
{
    private readonly ITicketRepository _ticketRepository = Substitute.For<ITicketRepository>();
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly TicketAnsweredEventHandler _sut;

    public TicketAnsweredEventHandlerTests()
    {
        _sut = new TicketAnsweredEventHandler(_ticketRepository, _notificationService, _auditService);
    }

    private static DomainEventNotification<TicketAnsweredEvent> BuildNotification(TicketId ticketId, UserId adminId) =>
        new(new TicketAnsweredEvent(ticketId, adminId));

    [Fact]
    public async Task Handle_WhenTicketNotFound_DoesNotSendNotificationOrAudit()
    {
        var ticketId = TicketId.NewId();
        _ticketRepository
            .GetByIdWithMessagesAsync(Arg.Any<TicketId>(), Arg.Any<CancellationToken>())
            .Returns((Ticket?)null);

        await _sut.Handle(BuildNotification(ticketId, UserId.NewId()), CancellationToken.None);

        await _notificationService.DidNotReceiveWithAnyArgs().CreateNotificationAsync(
            default!, default!, default!, default!, default!, default, default!, default);
        await _auditService.DidNotReceiveWithAnyArgs().LogSystemEventAsync(default!, default!, default);
    }

    [Fact]
    public async Task Handle_WhenTicketExists_SendsNotificationWithSubjectAndTicketIdAsEntity()
    {
        var ticketId = TicketId.NewId();
        var adminId = UserId.NewId();
        var ticket = new TicketBuilder().WithId(ticketId).WithSubject("پاسخگویی به مشکل پرداخت").Build();

        _ticketRepository
            .GetByIdWithMessagesAsync(Arg.Any<TicketId>(), Arg.Any<CancellationToken>())
            .Returns(ticket);

        await _sut.Handle(BuildNotification(ticketId, adminId), CancellationToken.None);

        await _notificationService.Received(1).CreateNotificationAsync(
            adminId,
            "پاسخ جدید به تیکت",
            Arg.Is<string>(m => m!.Contains("پاسخگویی به مشکل پرداخت")),
            "TicketReply",
            Arg.Is<string>(link => link!.Contains(ticketId.Value.ToString())),
            ticketId.Value,
            "Ticket",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTicketExists_LogsSystemAuditForAnsweredEvent()
    {
        var ticketId = TicketId.NewId();
        var ticket = new TicketBuilder().WithId(ticketId).Build();

        _ticketRepository
            .GetByIdWithMessagesAsync(Arg.Any<TicketId>(), Arg.Any<CancellationToken>())
            .Returns(ticket);

        await _sut.Handle(BuildNotification(ticketId, UserId.NewId()), CancellationToken.None);

        await _auditService.Received(1).LogSystemEventAsync(
            "Notification Answered",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNotificationServiceThrows_LogsSystemEventAndDoesNotPropagate()
    {
        var ticketId = TicketId.NewId();
        var ticket = new TicketBuilder().WithId(ticketId).Build();

        _ticketRepository
            .GetByIdWithMessagesAsync(Arg.Any<TicketId>(), Arg.Any<CancellationToken>())
            .Returns(ticket);

        _notificationService
            .CreateNotificationAsync(
                Arg.Any<UserId>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("smtp down"));

        await Should.NotThrowAsync(() =>
            _sut.Handle(BuildNotification(ticketId, UserId.NewId()), CancellationToken.None));

        await _auditService.Received().LogSystemEventAsync(
            "smtp down",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrows_LogsSystemEventAndDoesNotPropagate()
    {
        _ticketRepository
            .GetByIdWithMessagesAsync(Arg.Any<TicketId>(), Arg.Any<CancellationToken>())
            .Returns<Task<Ticket?>>(_ => throw new InvalidOperationException("db offline"));

        await Should.NotThrowAsync(() =>
            _sut.Handle(BuildNotification(TicketId.NewId(), UserId.NewId()), CancellationToken.None));

        await _auditService.Received().LogSystemEventAsync(
            "db offline",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
