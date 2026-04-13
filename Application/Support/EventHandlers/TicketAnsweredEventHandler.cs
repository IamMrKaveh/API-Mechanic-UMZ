using Domain.Support.Events;
using Domain.Support.Interfaces;
using Domain.Support.ValueObjects;

namespace Application.Support.EventHandlers;

public sealed class TicketAnsweredEventHandler(
    ITicketRepository ticketRepository,
    INotificationService notificationService,
    IAuditService auditService) : INotificationHandler<TicketAnsweredEvent>
{
    public async Task Handle(
        TicketAnsweredEvent notification,
        CancellationToken ct)
    {
        try
        {
            var ticketId = TicketId.From(notification.TicketId.Value);

            var ticket = await ticketRepository.GetByIdWithMessagesAsync(ticketId, ct);

            if (ticket is null) return;

            await notificationService.CreateNotificationAsync(
                notification.AdminId,
                "پاسخ جدید به تیکت",
                $"تیکت «{ticket.Subject}» پاسخ داده شد.",
                "TicketReply",
                $"/dashboard/tickets/{ticket.Id}",
                ticketId.Value,
                "Ticket",
                ct);

            await auditService.LogSystemEventAsync(
                "Notification Answered",
                "Notification sent for ticket {TicketId} answered by admin {AdminId}",
                ct);
        }
        catch (Exception ex)
        {
            await auditService.LogSystemEventAsync(
                ex.Message,
                $"Failed to handle TicketAnsweredEvent for ticket {notification.TicketId}",
                ct);
        }
    }
}