using Domain.Support.Events;
using Domain.Support.Interfaces;

namespace Application.Support.EventHandlers;

public sealed class TicketAnsweredEventHandler(
    ITicketRepository ticketRepository,
    INotificationService notificationService,
    ILogger<TicketAnsweredEventHandler> logger) : INotificationHandler<Domain.Support.Events.TicketAnsweredEvent>
{
    public async Task Handle(
        TicketAnsweredEvent notification,
        CancellationToken ct)
    {
        try
        {
            var ticket = await ticketRepository.GetByIdWithMessagesAsync(notification.TicketId, ct);

            if (ticket is null) return;

            await notificationService.CreateNotificationAsync(
                ticket.UserId,
                "پاسخ جدید به تیکت",
                $"تیکت «{ticket.Subject}» پاسخ داده شد.",
                "TicketReply",
                $"/dashboard/tickets/{ticket.Id}",
                ticket.Id,
                "Ticket",
                ct);

            logger.LogInformation(
                "Notification sent for ticket {TicketId} answered by admin {AdminId}",
                notification.TicketId,
                notification.AdminId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to handle TicketAnsweredEvent for ticket {TicketId}", notification.TicketId);
        }
    }
}