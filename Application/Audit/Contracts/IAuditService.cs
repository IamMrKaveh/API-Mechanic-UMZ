namespace Application.Audit.Contracts;

public interface IAuditService
{
    Task LogAsync(
        string eventType,
        string action,
        string ipAddress,
        Guid? userId = null,
        string? entityType = null,
        string? entityId = null,
        string? details = null,
        string? userAgent = null,
        CancellationToken ct = default);

    Task LogSecurityEventAsync(
        string action,
        string details,
        string ipAddress,
        Guid? userId = null,
        CancellationToken ct = default);

    Task LogOrderEventAsync(
        Guid orderId,
        string action,
        string ipAddress,
        Guid? userId = null,
        string? details = null,
        CancellationToken ct = default);

    Task LogPaymentEventAsync(
        Guid paymentId,
        string action,
        string ipAddress,
        Guid? userId = null,
        string? details = null,
        CancellationToken ct = default);
}