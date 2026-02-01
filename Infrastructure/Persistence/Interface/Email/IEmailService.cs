namespace Infrastructure.Persistence.Interface.Email;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    Task SendOrderConfirmationEmailAsync(string to, int orderId, decimal totalAmount);
    Task SendPasswordResetEmailAsync(string to, string resetToken);
    Task SendOrderStatusUpdateEmailAsync(string to, int orderId, string newStatus);
}