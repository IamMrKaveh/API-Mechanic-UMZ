using Domain.Support.Interfaces;

namespace Application.Support.EventHandlers;

public sealed class TicketAnsweredEventHandler(
    ITicketRepository ticketRepository,
    INotificationService notificationService,
    ILogger<TicketAnsweredEventHandler> logger) : INotificationHandler<Domain.Support.Events.TicketAnsweredEvent>
{
    private readonly ITicketRepository _ticketRepository = ticketRepository;
    private readonly INotificationService _notificationService = notificationService;
    private readonly ILogger<TicketAnsweredEventHandler> _logger = logger;

    public async Task Handle(
        Domain.Support.Events.TicketAnsweredEvent notification,
        CancellationToken ct)
    {
        try
        {
            var ticket = await _ticketRepository.GetByIdWithMessagesAsync(notification.TicketId, ct);

            if (ticket is null) return;

            await _notificationService.CreateNotificationAsync(
                ticket.UserId,
                "پاسخ جدید به تیکت",
                $"تیکت «{ticket.Subject}» پاسخ داده شد.",
                "TicketReply",
                $"/dashboard/tickets/{ticket.Id}",
                ticket.Id,
                "Ticket",
                ct);

            _logger.LogInformation(
                "Notification sent for ticket {TicketId} answered by admin {AdminId}",
                notification.TicketId,
                notification.AdminId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle TicketAnsweredEvent for ticket {TicketId}", notification.TicketId);
        }
    }
}