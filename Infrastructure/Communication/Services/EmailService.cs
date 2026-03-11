namespace Infrastructure.Communication.Services;

internal class EmailService(
    ILogger<EmailService> logger,
    IOptions<SmtpOptions> smtpOptions) : IEmailService
{
    private readonly ILogger<EmailService> _logger = logger;
    private readonly SmtpOptions _smtpOptions = smtpOptions.Value;

    public async Task SendEmailAsync(
        string to,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrEmpty(_smtpOptions.Host) || string.IsNullOrEmpty(_smtpOptions.Username))
            {
                _logger.LogWarning("SMTP settings not configured. Email not sent to {To}", to);
                return;
            }

            using var client = new System.Net.Mail.SmtpClient(_smtpOptions.Host, _smtpOptions.Port)
            {
                Credentials = new System.Net.NetworkCredential(_smtpOptions.Username, _smtpOptions.Password),
                EnableSsl = true
            };

            var message = new System.Net.Mail.MailMessage
            {
                From = new System.Net.Mail.MailAddress(
                    string.IsNullOrEmpty(_smtpOptions.FromEmail) ? _smtpOptions.Username : _smtpOptions.FromEmail,
                    _smtpOptions.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            message.To.Add(to);

            await client.SendMailAsync(message, ct);
            _logger.LogInformation("Email sent successfully to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
        }
    }

    public async Task SendOrderConfirmationEmailAsync(
        string to,
        int orderId,
        decimal totalAmount,
        CancellationToken ct = default)
    {
        var subject = $"تایید سفارش #{orderId}";
        var body = $@"
            <html dir='rtl'>
            <body style='font-family: Tahoma, Arial, sans-serif;'>
                <h2>سفارش شما با موفقیت ثبت شد</h2>
                <p>شماره سفارش: <strong>{orderId}</strong></p>
                <p>مبلغ کل: <strong>{totalAmount:N0} تومان</strong></p>
                <p>با تشکر از خرید شما</p>
            </body>
            </html>";

        await SendEmailAsync(to, subject, body, true, ct);
    }

    public async Task SendOrderStatusUpdateEmailAsync(
        string to,
        int orderId,
        string newStatus,
        CancellationToken ct = default)
    {
        var subject = $"بروزرسانی وضعیت سفارش #{orderId}";
        var body = $@"
            <html dir='rtl'>
            <body style='font-family: Tahoma, Arial, sans-serif;'>
                <h2>وضعیت سفارش شما تغییر کرد</h2>
                <p>شماره سفارش: <strong>{orderId}</strong></p>
                <p>وضعیت جدید: <strong>{newStatus}</strong></p>
            </body>
            </html>";

        await SendEmailAsync(to, subject, body, true, ct);
    }

    public async Task SendTicketAnsweredEmailAsync(
        string to,
        int ticketId,
        string ticketSubject,
        CancellationToken ct = default)
    {
        var subject = $"پاسخ جدید به تیکت: {ticketSubject}";
        var body = $@"
            <html dir='rtl'>
            <body style='font-family: Tahoma, Arial, sans-serif;'>
                <h2>تیکت شما پاسخ داده شد</h2>
                <p>شماره تیکت: <strong>{ticketId}</strong></p>
                <p>موضوع: <strong>{ticketSubject}</strong></p>
                <p>برای مشاهده پاسخ، به پنل کاربری خود مراجعه کنید.</p>
            </body>
            </html>";

        await SendEmailAsync(to, subject, body, true, ct);
    }
}