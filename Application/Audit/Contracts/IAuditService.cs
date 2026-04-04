using Application.Audit.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Audit.Contracts;

/// <summary>
/// سرویس ثبت لاگ‌های حسابرسی
/// </summary>
public interface IAuditService
{
    Task LogAsync(
        UserId? userId,
        string eventType,
        string action,
        string details,
        string? ipAddress = null,
        string? userAgent = null);

    Task LogUserActionAsync(
        UserId userId,
        string action,
        string details,
        string ipAddress,
        string? userAgent = null);

    Task LogSecurityEventAsync(
        string eventType,
        string details,
        string ipAddress,
        UserId? userId = null,
        string? userAgent = null);

    Task LogSystemEventAsync(
        string eventType,
        string details,
        UserId? userId = null,
        string? ipAddress = null,
        string? userAgent = null);

    Task LogAdminEventAsync(
        string action,
        UserId userId,
        string details,
        string? ipAddress = null,
        string? userAgent = null);

    Task LogOrderEventAsync(
        OrderId orderId,
        string action,
        UserId userId,
        string details);

    Task LogProductEventAsync(
        ProductId productId,
        string action,
        string details,
        UserId? userId = null);

    Task LogInventoryEventAsync(
        ProductId productId,
        string action,
        string details,
        UserId? userId = null);

    Task<(IEnumerable<AuditDtos> Logs, int TotalItems)> GetAuditLogsAsync(
        UserId? userId,
        string? eventType,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize);

    Task<byte[]> ExportToCsvAsync(
        AuditExportRequest request,
        CancellationToken ct = default);
}