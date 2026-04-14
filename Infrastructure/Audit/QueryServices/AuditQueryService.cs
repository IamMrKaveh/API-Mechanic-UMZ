using Application.Audit.Contracts;
using Application.Audit.Features.Shared;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Infrastructure.Audit.QueryServices;

public sealed class AuditQueryService(DBContext context) : IAuditQueryService
{
    public async Task<PaginatedResult<AuditLogDto>> GetAuditLogsAsync(
        UserId? userId,
        string? eventType,
        string? entityType,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = context.AuditLogs.AsNoTracking().AsQueryable();

        if (userId is not null)
            query = query.Where(l => l.UserId == userId);

        if (!string.IsNullOrEmpty(eventType))
            query = query.Where(l => l.EventType == eventType);

        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(l => l.EntityType == entityType);

        if (from.HasValue)
            query = query.Where(l => l.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(l => l.CreatedAt <= to.Value);

        var total = await query.CountAsync(ct);

        var logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new AuditLogDto
            {
                Id = l.Id.Value,
                UserId = l.UserId == null ? null : l.UserId.Value,
                EventType = l.EventType,
                Action = l.Action,
                Details = l.Details,
                IpAddress = l.IpAddress,
                UserAgent = l.UserAgent,
                EntityType = l.EntityType,
                CreatedAt = l.CreatedAt,
                Timestamp = l.CreatedAt,
                IsArchived = l.IsArchived
            })
            .ToListAsync(ct);

        return new PaginatedResult<AuditLogDto>
        {
            Items = logs,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetByEntityAsync(
        string entityType,
        string entityId,
        CancellationToken ct = default)
    {
        var logs = await context.AuditLogs
            .AsNoTracking()
            .Where(l => l.EntityType == entityType && l.EntityId == entityId)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new AuditLogDto
            {
                Id = l.Id.Value,
                UserId = l.UserId == null ? null : l.UserId.Value,
                EventType = l.EventType,
                Action = l.Action,
                Details = l.Details,
                IpAddress = l.IpAddress,
                UserAgent = l.UserAgent,
                EntityType = l.EntityType,
                EntityId = l.EntityId == null ? null : Guid.Parse(l.EntityId),
                CreatedAt = l.CreatedAt,
                Timestamp = l.CreatedAt,
                IsArchived = l.IsArchived
            })
            .ToListAsync(ct);

        return logs.AsReadOnly();
    }

    public async Task<(IReadOnlyList<AuditLogDto> Logs, int Total)> SearchAsync(
        AuditSearchRequest request,
        CancellationToken ct = default)
    {
        var query = context.AuditLogs.AsNoTracking().AsQueryable();

        if (request.UserId.HasValue)
            query = query.Where(l => l.UserId!.Value == request.UserId.Value);

        if (!string.IsNullOrEmpty(request.EventType))
            query = query.Where(l => l.EventType == request.EventType);

        if (!string.IsNullOrEmpty(request.Action))
            query = query.Where(l => l.Action.Contains(request.Action));

        if (!string.IsNullOrEmpty(request.EntityName))
            query = query.Where(l => l.EntityType == request.EntityName);

        if (!string.IsNullOrEmpty(request.Keyword))
            query = query.Where(l => (l.Details != null && l.Details.Contains(request.Keyword))
                                  || l.Action.Contains(request.Keyword));

        if (!string.IsNullOrEmpty(request.IpAddress))
            query = query.Where(l => l.IpAddress == request.IpAddress);

        var fromDate = request.From ?? request.FromDate;
        var toDate = request.To ?? request.ToDate;

        if (fromDate.HasValue)
            query = query.Where(l => l.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(l => l.CreatedAt <= toDate.Value);

        var total = await query.CountAsync(ct);

        var ordered = request.SortDesc
            ? query.OrderByDescending(l => l.CreatedAt)
            : query.OrderBy(l => l.CreatedAt);

        var logs = await ordered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(l => new AuditLogDto
            {
                Id = l.Id.Value,
                UserId = l.UserId == null ? null : l.UserId.Value,
                EventType = l.EventType,
                Action = l.Action,
                Details = l.Details,
                IpAddress = l.IpAddress,
                UserAgent = l.UserAgent,
                EntityType = l.EntityType,
                CreatedAt = l.CreatedAt,
                Timestamp = l.CreatedAt,
                IsArchived = l.IsArchived
            })
            .ToListAsync(ct);

        return (logs.AsReadOnly(), total);
    }

    public async Task<byte[]> ExportToCsvAsync(
        AuditExportRequest request,
        CancellationToken ct = default)
    {
        var query = context.AuditLogs.AsNoTracking().AsQueryable();

        if (request.UserId.HasValue)
            query = query.Where(l => l.UserId!.Value == request.UserId.Value);

        if (!string.IsNullOrEmpty(request.EventType))
            query = query.Where(l => l.EventType == request.EventType);

        if (!string.IsNullOrEmpty(request.Action))
            query = query.Where(l => l.Action.Contains(request.Action));

        var fromDate = request.From ?? request.FromDate;
        var toDate = request.To ?? request.ToDate;

        if (fromDate.HasValue)
            query = query.Where(l => l.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(l => l.CreatedAt <= toDate.Value);

        var maxRows = request.MaxRows > 0 ? request.MaxRows : 10000;

        var logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .Take(maxRows)
            .Select(l => new
            {
                l.Id,
                UserId = l.UserId == null ? string.Empty : l.UserId.Value.ToString(),
                l.EventType,
                l.Action,
                l.IpAddress,
                l.EntityType,
                l.EntityId,
                CreatedAt = l.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            })
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("Id,UserId,EventType,Action,IpAddress,EntityType,EntityId,CreatedAt");

        foreach (var log in logs)
        {
            sb.AppendLine($"{log.Id},{log.UserId},{Escape(log.EventType)},{Escape(log.Action)},{log.IpAddress},{log.EntityType},{log.EntityId},{log.CreatedAt}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Contains(',') ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
    }
}