namespace Application.Communication.Contracts;

public interface IEmailService
{
    Task SendEmailAsync(
        string to,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken ct = default
        );

    Task SendOrderConfirmationEmailAsync(
        string to,
        int orderId,
        decimal totalAmount,
        CancellationToken ct = default
        );

    Task SendOrderStatusUpdateEmailAsync(
        string to,
        int orderId,
        string newStatus,
        CancellationToken ct = default
        );

    Task SendTicketAnsweredEmailAsync(
        string to,
        int ticketId,
        string ticketSubject,
        CancellationToken ct = default
        );
}