using Domain.Common.ValueObjects;
using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Audit.Contracts;

public interface IAuditService
{
    Task LogAsync(
        string eventType,
        string action,
        IpAddress ipAddress,
        UserId? userId = null,
        string? entityType = null,
        string? entityId = null,
        string? details = null,
        string? userAgent = null,
        CancellationToken ct = default);

    Task LogSecurityEventAsync(
        string action,
        string details,
        IpAddress ipAddress,
        UserId? userId = null,
        CancellationToken ct = default);

    Task LogOrderEventAsync(
        OrderId orderId,
        string action,
        IpAddress ipAddress,
        UserId? userId = null,
        string? details = null,
        CancellationToken ct = default);

    Task LogPaymentEventAsync(
        PaymentTransactionId paymentId,
        string action,
        IpAddress ipAddress,
        Guid? userId = null,
        string? details = null,
        CancellationToken ct = default);

    Task LogInventoryEventAsync(
        ProductId productId,
        string action,
        string details,
        UserId? userId = null);
}