namespace Infrastructure.Communication.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken ct = default)
    {
        try
        {
            var smtpSettings = _configuration.GetSection("Smtp");
            var host = smtpSettings["Host"];
            var port = int.Parse(smtpSettings["Port"] ?? "587");
            var username = smtpSettings["Username"];
            var password = smtpSettings["Password"];
            var fromEmail = smtpSettings["FromEmail"];
            var fromName = smtpSettings["FromName"];

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username))
            {
                _logger.LogWarning("SMTP settings not configured. Email not sent to {To}", to);
                return;
            }

            using var client = new System.Net.Mail.SmtpClient(host, port)
            {
                Credentials = new System.Net.NetworkCredential(username, password),
                EnableSsl = true
            };

            var message = new System.Net.Mail.MailMessage
            {
                From = new System.Net.Mail.MailAddress(fromEmail ?? username, fromName ?? "Ledka"),
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

    public async Task SendOrderConfirmationEmailAsync(string to, int orderId, decimal totalAmount, CancellationToken ct = default)
    {
        var subject = $"تایید سفارش #{orderId}";
        var body = $@"
            <html dir='rtl'>
            <body style='font-family: Tahoma, Arial, sans-serif;'>
                <h2>سفارش شما با موفقیت ثبت شد</h2>
                <p>شماره سفارش: <strong>{orderId}</strong></p>
                <p>مبلغ کل: <strong>{totalAmount:N0} تومان</strong></p>
                <p>با تشکر از خرید شما</p>
                <p>فروشگاه لدکا</p>
            </body>
            </html>";

        await SendEmailAsync(to, subject, body, true, ct);
    }

    public async Task SendOrderStatusUpdateEmailAsync(string to, int orderId, string newStatus, CancellationToken ct = default)
    {
        var subject = $"بروزرسانی وضعیت سفارش #{orderId}";
        var body = $@"
            <html dir='rtl'>
            <body style='font-family: Tahoma, Arial, sans-serif;'>
                <h2>وضعیت سفارش شما تغییر کرد</h2>
                <p>شماره سفارش: <strong>{orderId}</strong></p>
                <p>وضعیت جدید: <strong>{newStatus}</strong></p>
                <p>فروشگاه لدکا</p>
            </body>
            </html>";

        await SendEmailAsync(to, subject, body, true, ct);
    }

    public async Task SendTicketAnsweredEmailAsync(string to, int ticketId, string ticketSubject, CancellationToken ct = default)
    {
        var subject = $"پاسخ جدید به تیکت: {ticketSubject}";
        var body = $@"
            <html dir='rtl'>
            <body style='font-family: Tahoma, Arial, sans-serif;'>
                <h2>تیکت شما پاسخ داده شد</h2>
                <p>شماره تیکت: <strong>{ticketId}</strong></p>
                <p>موضوع: <strong>{ticketSubject}</strong></p>
                <p>برای مشاهده پاسخ، به پنل کاربری خود مراجعه کنید.</p>
                <p>فروشگاه لدکا</p>
            </body>
            </html>";

        await SendEmailAsync(to, subject, body, true, ct);
    }
}