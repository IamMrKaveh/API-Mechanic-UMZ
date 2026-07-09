using Application.Wallet.Contracts;
using Application.Wallet.Features.Shared;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;

namespace Infrastructure.Wallet.QueryServices;

public sealed class WalletFraudAlertQueryService(DBContext context) : IWalletFraudAlertQueryService
{
    public async Task<PaginatedResult<WalletFraudAlertDto>> GetAlertsPageAsync(
        FraudAlertStatus? status,
        FraudAlertSeverity? severity,
        Guid? userId,
        int page,
        int pageSize,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 200) pageSize = 200;

        var query = context.Set<WalletFraudAlert>().AsNoTracking();

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        if (severity.HasValue)
            query = query.Where(a => a.Severity == severity.Value);

        if (userId.HasValue)
        {
            var uid = userId.Value;
            query = query.Where(a => a.UserId.Value == uid);
        }

        if (fromDate.HasValue)
        {
            var from = DateTime.SpecifyKind(fromDate.Value, DateTimeKind.Utc);
            query = query.Where(a => a.TriggeredAt >= from);
        }

        if (toDate.HasValue)
        {
            var to = DateTime.SpecifyKind(toDate.Value, DateTimeKind.Utc);
            query = query.Where(a => a.TriggeredAt <= to);
        }

        var totalCount = await query.CountAsync(ct);

        var alerts = await query
            .OrderByDescending(a => a.TriggeredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var userIds = alerts.Select(a => a.UserId.Value).Distinct().ToList();

        var userNames = await context.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id.Value))
            .Select(u => new { u.Id, FullName = $"{u.FullName.FirstName} {u.FullName.LastName}" })
            .ToDictionaryAsync(x => x.Id.Value, x => x.FullName, ct);

        var dtos = alerts.Select(a => new WalletFraudAlertDto(
            a.Id.Value,
            a.WalletId.Value,
            a.UserId.Value,
            userNames.TryGetValue(a.UserId.Value, out var name) ? name : null,
            a.RuleName,
            a.Severity.ToString(),
            a.Description,
            a.Metadata,
            a.Status.ToString(),
            a.TriggeredAt,
            a.ReviewedBy?.Value,
            a.ReviewedAt,
            a.ReviewNote,
            a.CreatedAt)).ToList();

        return PaginatedResult<WalletFraudAlertDto>.Create(dtos, totalCount, page, pageSize);
    }

    public async Task<WalletFraudAlertDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var alert = await context.Set<WalletFraudAlert>()
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id.Value == id, ct);

        if (alert is null) return null;

        var userFullName = await context.Users
            .AsNoTracking()
            .Where(u => u.Id == alert.UserId)
            .Select(u => $"{u.FullName.FirstName} {u.FullName.LastName}")
            .FirstOrDefaultAsync(ct);

        return new WalletFraudAlertDto(
            alert.Id.Value,
            alert.WalletId.Value,
            alert.UserId.Value,
            userFullName,
            alert.RuleName,
            alert.Severity.ToString(),
            alert.Description,
            alert.Metadata,
            alert.Status.ToString(),
            alert.TriggeredAt,
            alert.ReviewedBy?.Value,
            alert.ReviewedAt,
            alert.ReviewNote,
            alert.CreatedAt);
    }

    public async Task<int> GetOpenAlertsCountAsync(CancellationToken ct = default)
        => await context.Set<WalletFraudAlert>()
            .AsNoTracking()
            .CountAsync(a => a.Status == FraudAlertStatus.Open, ct);
}