namespace Application.Communication.Contracts;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body, bool isHtml = true, CancellationToken ct = default);

    Task SendTemplateAsync(string to, string templateName, object model, CancellationToken ct = default);

    Task SendOrderConfirmationAsync(string to, string orderNumber, decimal amount, CancellationToken ct = default);

    Task SendPaymentReceiptAsync(string to, string orderNumber, decimal amount, long refId, CancellationToken ct = default);

    Task SendPasswordResetAsync(string to, string resetLink, CancellationToken ct = default);
}