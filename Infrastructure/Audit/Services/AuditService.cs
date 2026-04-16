using Domain.Audit.Entities;
using Domain.Audit.Interfaces;
using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Infrastructure.Audit.Services;

public sealed class AuditService(
    IAuditRepository auditRepository,
    IAuditMaskingService maskingService,
    IHttpContextAccessor httpContextAccessor,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IAuditService
{
    public async Task LogAsync(
        string eventType,
        string action,
        IpAddress ipAddress,
        UserId? userId = null,
        string? entityType = null,
        string? entityId = null,
        string? details = null,
        string? userAgent = null,
        CancellationToken ct = default)
    {
        try
        {
            var maskedDetails = details is not null ? maskingService.MaskSensitiveData(details) : null;

            var log = AuditLog.Create(
                userId,
                eventType,
                action,
                ipAddress.Value,
                entityType,
                entityId,
                maskedDetails,
                userAgent ?? GetUserAgent());

            await auditRepository.AddAuditLogAsync(log, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            await auditService.LogSystemEventAsync(
                ex.Message,
                "Failed to write audit log: EventType={eventType}, Action={action}",
                ct);
        }
    }

    public async Task LogInformationAsync(
    string details,
    CancellationToken ct = default) => await LogAsync("Information", "", IpAddress.System, null, null, null, details, null, ct);

    public async Task LogDebugAsync(
    string details,
    CancellationToken ct = default) => await LogAsync("Debug", "", IpAddress.System, null, null, null, details, null, ct);

    public async Task LogWarningAsync(
    string details,
    CancellationToken ct = default) => await LogAsync("Warning", "", IpAddress.System, null, null, null, details, null, ct);

    public async Task LogErrorAsync(
    string details,
    CancellationToken ct = default) => await LogAsync("Error", "", IpAddress.System, null, null, null, details, null, ct);

    public async Task LogSecurityEventAsync(
        string action,
        string details,
        IpAddress ipAddress,
        UserId? userId = null,
        CancellationToken ct = default) => await LogAsync("SecurityEvent", action, ipAddress, userId, null, null, details, null, ct);

    public async Task LogSystemEventAsync(
        string action,
        string details,
        CancellationToken ct = default) => await LogAsync("SystemEvent", action, IpAddress.System, null, null, null, details, null, ct);

    public async Task LogOrderEventAsync(
        OrderId orderId,
        string action,
        IpAddress ipAddress,
        UserId? userId = null,
        string? details = null,
        CancellationToken ct = default) => await LogAsync("OrderEvent", action, ipAddress, userId, "Order", orderId.Value.ToString(), details, null, ct);

    public async Task LogPaymentEventAsync(
        PaymentTransactionId paymentId,
        string action,
        IpAddress ipAddress,
        UserId? userId = null,
        string? details = null,
        CancellationToken ct = default) => await LogAsync("PaymentEvent", action, ipAddress, userId, "Payment", paymentId.Value.ToString(), details, null, ct);

    public async Task LogInventoryEventAsync(
        ProductId productId,
        string action,
        string details,
        UserId? userId = null) => await LogAsync("InventoryEvent", action, IpAddress.System, userId, "Product", productId.Value.ToString(), details);

    public async Task LogInventoryEventAsync(
        VariantId variantId,
        string action,
        string details,
        UserId? userId = null) => await LogAsync("InventoryEvent", action, IpAddress.System, userId, "Variant", variantId.Value.ToString(), details);

    public async Task LogProductEventAsync(
        ProductId productId,
        string action,
        string details,
        UserId userId) => await LogAsync("ProductEvent", action, IpAddress.System, userId, "Product", productId.Value.ToString(), details);

    public async Task LogAdminEventAsync(string title, UserId adminId, string detail) => await LogAsync("AdminEvent", title, IpAddress.System, adminId, null, null, detail);

    private string GetUserAgent() => httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString() ?? string.Empty;
}