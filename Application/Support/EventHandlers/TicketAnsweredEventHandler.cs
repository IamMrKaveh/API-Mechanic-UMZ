namespace Application.Support.EventHandlers;

/// <summary>
/// وقتی تیکت پاسخ داده شد، ایمیل و نوتیفیکیشن ارسال می‌شود
/// </summary>
public sealed class TicketAnsweredEventHandler : INotificationHandler<Domain.Support.Events.TicketAnsweredEvent>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly ILogger<TicketAnsweredEventHandler> _logger;

    public TicketAnsweredEventHandler(
        ITicketRepository ticketRepository,
        INotificationService notificationService,
        IEmailService emailService,
        ILogger<TicketAnsweredEventHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _notificationService = notificationService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(Domain.Support.Events.TicketAnsweredEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var ticket = await _ticketRepository.GetByIdWithMessagesAsync(notification.TicketId, cancellationToken);
            if (ticket is null) return;

            // ارسال نوتیفیکیشن درون‌برنامه‌ای
            await _notificationService.CreateNotificationAsync(
                ticket.UserId,
                "پاسخ جدید به تیکت",
                $"تیکت «{ticket.Subject}» پاسخ داده شد.",
                "TicketReply",
                $"/dashboard/tickets/{ticket.Id}",
                ticket.Id,
                "Ticket",
                cancellationToken);

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