using Application.Communication.Contracts;
using Infrastructure.Communication.Options;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Communication.Services;

internal sealed class EmailService(
    IOptions<SmtpOptions> smtpOptions,
    ILogger<EmailService> logger) : IEmailService
{
    private readonly SmtpOptions _smtp = smtpOptions.Value;

    public async Task SendAsync(string to, string subject, string body, bool isHtml = true, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrEmpty(_smtp.Host))
            {
                logger.LogWarning("SMTP not configured. Email not sent to {To}", to);
                return;
            }

            using var client = new System.Net.Mail.SmtpClient(_smtp.Host, _smtp.Port)
            {
                Credentials = new System.Net.NetworkCredential(_smtp.Username, _smtp.Password),
                EnableSsl = true
            };

            var fromAddress = string.IsNullOrEmpty(_smtp.FromEmail) ? _smtp.Username : _smtp.FromEmail;
            var msg = new System.Net.Mail.MailMessage
            {
                From = new System.Net.Mail.MailAddress(fromAddress, _smtp.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };
            msg.To.Add(to);

            await client.SendMailAsync(msg, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {To}", to);
        }
    }

    public async Task SendTemplateAsync(string to, string templateName, object model, CancellationToken ct = default)
    {
        var body = $"Template: {templateName} | Data: {System.Text.Json.JsonSerializer.Serialize(model)}";
        await SendAsync(to, templateName, body, false, ct);
    }

    public async Task SendOrderConfirmationAsync(string to, string orderNumber, Money amount, CancellationToken ct = default)
    {
        var subject = $"تأیید سفارش #{orderNumber}";
        var body = $@"<html dir='rtl'><body>
            <h2>سفارش شما با موفقیت ثبت شد</h2>
            <p>شماره سفارش: <strong>{orderNumber}</strong></p>
            <p>مبلغ کل: <strong>{amount.Amount:N0} تومان</strong></p>
        </body></html>";
        await SendAsync(to, subject, body, true, ct);
    }

    public async Task SendPaymentReceiptAsync(string to, string orderNumber, Money amount, long refId, CancellationToken ct = default)
    {
        var subject = $"رسید پرداخت سفارش #{orderNumber}";
        var body = $@"<html dir='rtl'><body>
            <h2>پرداخت شما با موفقیت انجام شد</h2>
            <p>شماره سفارش: <strong>{orderNumber}</strong></p>
            <p>مبلغ: <strong>{amount.Amount:N0} تومان</strong></p>
            <p>کد پیگیری: <strong>{refId}</strong></p>
        </body></html>";
        await SendAsync(to, subject, body, true, ct);
    }

    public async Task SendPasswordResetAsync(string to, string resetLink, CancellationToken ct = default)
    {
        var subject = "بازیابی رمز عبور";
        var body = $@"<html dir='rtl'><body>
            <h2>بازیابی رمز عبور</h2>
            <p>برای بازیابی رمز عبور روی لینک زیر کلیک کنید:</p>
            <a href='{resetLink}'>{resetLink}</a>
        </body></html>";
        await SendAsync(to, subject, body, true, ct);
    }
}