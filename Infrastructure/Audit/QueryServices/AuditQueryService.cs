using Application.Audit.Features.Shared;
using Domain.Audit.Entities;
using Domain.Audit.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Audit.QueryServices;

public sealed class AuditQueryService(DBContext context) : IAuditQueryService
{
    private static readonly JsonSerializerOptions JsonExportOptions = new()
    {
        WriteIndented = true
    };

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
                EntityId = l.EntityId,
                CreatedAt = l.CreatedAt,
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

    public async Task<AuditLogDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var logId = AuditLogId.From(id);

        var detail = await (
            from l in context.AuditLogs.AsNoTracking()
            where l.Id == logId
            join u in context.Users.AsNoTracking()
                on l.UserId equals u.Id into userJoin
            from u in userJoin.DefaultIfEmpty()
            select new AuditLogDetailDto
            {
                Id = l.Id.Value,
                UserId = l.UserId == null ? null : l.UserId.Value,
                UserName = u == null
                    ? null
                    : $"{u.FullName.FirstName} {u.FullName.LastName}",
                EventType = l.EventType,
                Action = l.Action,
                Details = l.Details,
                IpAddress = l.IpAddress,
                UserAgent = l.UserAgent,
                EntityType = l.EntityType,
                EntityId = l.EntityId,
                CreatedAt = l.CreatedAt,
                IsArchived = l.IsArchived,
                ArchivedAt = l.ArchivedAt,
                IntegrityHash = l.IntegrityHash
            }).FirstOrDefaultAsync(ct);

        return detail;
    }

    public async Task<(IReadOnlyList<AuditLogDto> Logs, int Total)> SearchAsync(
        AuditSearchRequest request,
        CancellationToken ct = default)
    {
        var query = BuildSearchQuery(request);

        var total = await query.CountAsync(ct);

        var ordered = ApplySorting(query, request.SortBy, request.SortDesc);

        var pagedIds = await ordered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(l => l.Id)
            .ToListAsync(ct);

        var pageQuery = context.AuditLogs.AsNoTracking()
            .Where(l => pagedIds.Contains(l.Id));

        var pageQueryOrdered = ApplySorting(pageQuery, request.SortBy, request.SortDesc);

        var logs = await (
            from l in pageQueryOrdered
            join u in context.Users.AsNoTracking()
                on l.UserId equals u.Id into userJoin
            from u in userJoin.DefaultIfEmpty()
            select new AuditLogDto
            {
                Id = l.Id.Value,
                UserId = l.UserId == null ? null : l.UserId.Value,
                UserName = u == null
                    ? null
                    : $"{u.FullName.FirstName} {u.FullName.LastName}",
                EventType = l.EventType,
                Action = l.Action,
                Details = l.Details,
                IpAddress = l.IpAddress,
                UserAgent = l.UserAgent,
                EntityType = l.EntityType,
                CreatedAt = l.CreatedAt,
                IsArchived = l.IsArchived
            }).ToListAsync(ct);

        return (logs.AsReadOnly(), total);
    }

    public async Task<byte[]> ExportToCsvAsync(
        AuditExportRequest request,
        CancellationToken ct = default)
    {
        var query = BuildExportQuery(request);

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

    public async Task<byte[]> ExportToJsonAsync(
        AuditExportRequest request,
        CancellationToken ct = default)
    {
        var query = BuildExportQuery(request);

        var maxRows = request.MaxRows > 0 ? request.MaxRows : 5000;

        var logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .Take(maxRows)
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
                EntityId = l.EntityId,
                CreatedAt = l.CreatedAt,
                IsArchived = l.IsArchived
            })
            .ToListAsync(ct);

        return JsonSerializer.SerializeToUtf8Bytes(logs, JsonExportOptions);
    }

    public async Task<AuditStatisticsDto> GetStatisticsAsync(
        DateTime? from,
        DateTime? to,
        CancellationToken ct = default)
    {
        var query = context.AuditLogs.AsNoTracking().AsQueryable();

        if (from.HasValue)
            query = query.Where(l => l.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(l => l.CreatedAt <= to.Value);

        var totalLogs = await query.LongCountAsync(ct);

        var byEventType = await query
            .GroupBy(l => l.EventType)
            .Select(g => new { g.Key, Count = g.LongCount() })
            .ToDictionaryAsync(g => g.Key, g => g.Count, ct);

        var byHour = await query
            .GroupBy(l => l.CreatedAt.Hour)
            .Select(g => new { g.Key, Count = g.LongCount() })
            .ToDictionaryAsync(g => g.Key.ToString(), g => g.Count, ct);

        return new AuditStatisticsDto
        {
            TotalLogs = totalLogs,
            ByEventType = byEventType,
            ByHour = byHour
        };
    }

    private IQueryable<AuditLog> BuildSearchQuery(AuditSearchRequest request)
    {
        var query = context.AuditLogs.AsNoTracking().AsQueryable();

        if (request.UserId.HasValue)
        {
            var typedUserId = UserId.From(request.UserId.Value);
            query = query.Where(l => l.UserId != null && l.UserId == typedUserId);
        }

        if (!string.IsNullOrEmpty(request.EventType))
            query = query.Where(l => l.EventType == request.EventType);

        if (!string.IsNullOrEmpty(request.EntityType))
            query = query.Where(l => l.EntityType == request.EntityType);

        if (!string.IsNullOrEmpty(request.Action))
            query = query.Where(l => l.Action.Contains(request.Action));

        if (!string.IsNullOrEmpty(request.Keyword))
            query = query.Where(l => (l.Details != null && l.Details.Contains(request.Keyword))
                                  || l.Action.Contains(request.Keyword));

        if (!string.IsNullOrEmpty(request.IpAddress))
            query = query.Where(l => l.IpAddress == request.IpAddress);

        if (request.From.HasValue)
            query = query.Where(l => l.CreatedAt >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(l => l.CreatedAt <= request.To.Value);

        return query;
    }

    private IQueryable<AuditLog> BuildExportQuery(AuditExportRequest request)
    {
        var query = context.AuditLogs.AsNoTracking().AsQueryable();

        if (request.UserId.HasValue)
        {
            var typedUserId = UserId.From(request.UserId.Value);
            query = query.Where(l => l.UserId != null && l.UserId == typedUserId);
        }

        if (!string.IsNullOrEmpty(request.EventType))
            query = query.Where(l => l.EventType == request.EventType);

        if (!string.IsNullOrEmpty(request.EntityType))
            query = query.Where(l => l.EntityType == request.EntityType);

        if (!string.IsNullOrEmpty(request.Action))
            query = query.Where(l => l.Action.Contains(request.Action));

        if (request.From.HasValue)
            query = query.Where(l => l.CreatedAt >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(l => l.CreatedAt <= request.To.Value);

        return query;
    }

    private static IQueryable<AuditLog> ApplySorting(IQueryable<AuditLog> query, string? sortBy, bool desc)
    {
        return (sortBy?.ToLowerInvariant(), desc) switch
        {
            ("eventtype", true) => query.OrderByDescending(l => l.EventType).ThenByDescending(l => l.CreatedAt),
            ("eventtype", false) => query.OrderBy(l => l.EventType).ThenBy(l => l.CreatedAt),
            ("action", true) => query.OrderByDescending(l => l.Action).ThenByDescending(l => l.CreatedAt),
            ("action", false) => query.OrderBy(l => l.Action).ThenBy(l => l.CreatedAt),
            (_, true) => query.OrderByDescending(l => l.CreatedAt),
            (_, false) => query.OrderBy(l => l.CreatedAt),
        };
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Contains(',') ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
    }
}
