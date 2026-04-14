using Application.Audit.Contracts;
using Application.Audit.Features.Shared;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Audit.QueryServices;

public sealed class AuditQueryService(DBContext context) : IAuditQueryService
{
    public async Task<(IEnumerable<AuditLogDto> Logs, int Total)> GetAuditLogsAsync(
        UserId? userId,
        DateTime? from,
        DateTime? to,
        string? type,
        int page,
        int size,
        CancellationToken ct = default)
    {
        var query = context.AuditLogs.AsNoTracking().AsQueryable();

        if (userId is not null)
            query = query.Where(l => l.UserId == userId);

        if (from.HasValue)
            query = query.Where(l => l.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(l => l.CreatedAt <= to.Value);

        if (!string.IsNullOrEmpty(type))
            query = query.Where(l => l.EventType == type);

        var total = await query.CountAsync(ct);

        var logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(l => new AuditLogDto
            {
                Id = l.Id.Value,
                UserId = l.UserId == null ? null : l.UserId.Value,
                EventType = l.EventType,
                Action = l.Action,
                Details = l.Details,
                IpAddress = l.IpAddress,
                UserAgent = l.UserAgent,
                Timestamp = l.CreatedAt,
                CreatedAt = l.CreatedAt,
                IsArchived = l.IsArchived
            })
            .ToListAsync(ct);

        return (logs, total);
    }

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
                CreatedAt = l.CreatedAt,
                Timestamp = l.CreatedAt,
                IsArchived = l.IsArchived
            })
            .ToListAsync(ct);

        return PaginatedResult<AuditLogDto>.Create(logs, total, page, pageSize);
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetByEntityAsync(
        string entityType,
        string entityId,
        CancellationToken ct = default)
    {
        var results = await context.AuditLogs
            .AsNoTracking()
            .Where(l => l.EntityType == entityType && l.EntityId == entityId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(100)
            .Select(l => new AuditLogDto
            {
                Id = l.Id.Value,
                UserId = l.UserId == null ? null : l.UserId.Value,
                EventType = l.EventType,
                Action = l.Action,
                Details = l.Details,
                IpAddress = l.IpAddress,
                CreatedAt = l.CreatedAt,
                Timestamp = l.CreatedAt
            })
            .ToListAsync(ct);

        return results.AsReadOnly();
    }

    public async Task<(IEnumerable<AuditLogDto> Logs, int Total)> SearchAsync(
        AuditSearchRequest request,
        CancellationToken ct = default)
    {
        var query = context.AuditLogs.AsNoTracking().AsQueryable();

        if (request.UserId.HasValue)
            query = query.Where(l => l.UserId == UserId.From(request.UserId.Value));

        if (!string.IsNullOrEmpty(request.EventType))
            query = query.Where(l => l.EventType == request.EventType);

        if (!string.IsNullOrEmpty(request.Action))
            query = query.Where(l => l.Action.Contains(request.Action));

        if (!string.IsNullOrEmpty(request.IpAddress))
            query = query.Where(l => l.IpAddress == request.IpAddress);

        if (request.From.HasValue)
            query = query.Where(l => l.CreatedAt >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(l => l.CreatedAt <= request.To.Value);

        if (!string.IsNullOrEmpty(request.Keyword))
            query = query.Where(l => (l.Details != null && l.Details.Contains(request.Keyword))
                                  || l.Action.Contains(request.Keyword));

        query = request.SortDesc
            ? query.OrderByDescending(l => l.CreatedAt)
            : query.OrderBy(l => l.CreatedAt);

        var total = await query.CountAsync(ct);

        var logs = await query
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
                CreatedAt = l.CreatedAt,
                Timestamp = l.CreatedAt,
                IsArchived = l.IsArchived
            })
            .ToListAsync(ct);

        return (logs, total);
    }
}