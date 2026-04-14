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
    ILogger<AuditService> logger) : IAuditService
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
            logger.LogError(ex, "Failed to write audit log: EventType={EventType}, Action={Action}", eventType, action);
        }
    }

    public async Task LogSecurityEventAsync(
        string action,
        string details,
        IpAddress ipAddress,
        UserId? userId = null,
        CancellationToken ct = default)
    {
        await LogAsync("SecurityEvent", action, ipAddress, userId, null, null, details, null, ct);
    }

    public async Task LogSystemEventAsync(
        string action,
        string details,
        CancellationToken ct = default)
    {
        await LogAsync("SystemEvent", action, IpAddress.System, null, null, null, details, null, ct);
    }

    public async Task LogOrderEventAsync(
        OrderId orderId,
        string action,
        IpAddress ipAddress,
        UserId? userId = null,
        string? details = null,
        CancellationToken ct = default)
    {
        await LogAsync("OrderEvent", action, ipAddress, userId, "Order", orderId.Value.ToString(), details, null, ct);
    }

    public async Task LogPaymentEventAsync(
        PaymentTransactionId paymentId,
        string action,
        IpAddress ipAddress,
        Guid? userId = null,
        string? details = null,
        CancellationToken ct = default)
    {
        var uid = userId.HasValue ? UserId.From(userId.Value) : null;
        await LogAsync("PaymentEvent", action, ipAddress, uid, "Payment", paymentId.Value.ToString(), details, null, ct);
    }

    public async Task LogInventoryEventAsync(
        ProductId productId,
        string action,
        string details,
        UserId? userId = null)
    {
        await LogAsync("InventoryEvent", action, IpAddress.System, userId, "Product", productId.Value.ToString(), details);
    }

    public async Task LogInventoryEventAsync(
        VariantId variantId,
        string action,
        string details,
        UserId? userId = null)
    {
        await LogAsync("InventoryEvent", action, IpAddress.System, userId, "Variant", variantId.Value.ToString(), details);
    }

    public async Task LogProductEventAsync(
        ProductId productId,
        string action,
        string details,
        UserId userId)
    {
        await LogAsync("ProductEvent", action, IpAddress.System, userId, "Product", productId.Value.ToString(), details);
    }

    public async Task LogAdminEventAsync(string title, UserId adminId, string detail)
    {
        await LogAsync("AdminEvent", title, IpAddress.System, adminId, null, null, detail);
    }

    private string GetUserAgent()
    {
        return httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? string.Empty;
    }
}