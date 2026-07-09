using Application.Wallet.Features.Shared;
using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;

namespace Infrastructure.Wallet.QueryServices;

public sealed class WalletWithdrawalQueryService(DBContext context) : IWalletWithdrawalQueryService
{
    public async Task<PaginatedResult<WalletWithdrawalRequestDto>> GetByUserAsync(
        UserId userId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

        var baseQuery = context.Set<WalletWithdrawalRequest>()
            .AsNoTracking()
            .Where(w => w.UserId == userId);

        var total = await baseQuery.CountAsync(ct);

        var entities = await baseQuery
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = await MapToDtosAsync(entities, ct);

        return PaginatedResult<WalletWithdrawalRequestDto>.Create(items, total, page, pageSize);
    }

    public async Task<PaginatedResult<WalletWithdrawalRequestDto>> GetByStatusAsync(
        WithdrawalStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var baseQuery = context.Set<WalletWithdrawalRequest>().AsNoTracking();

        if (status.HasValue)
            baseQuery = baseQuery.Where(w => w.Status == status);

        var total = await baseQuery.CountAsync(ct);

        var entities = await baseQuery
            .OrderBy(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = await MapToDtosAsync(entities, ct);

        return PaginatedResult<WalletWithdrawalRequestDto>.Create(items, total, page, pageSize);
    }

    public async Task<WalletWithdrawalRequestDto?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var entity = await context.Set<WalletWithdrawalRequest>()
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id, ct);

        if (entity is null)
            return null;

        var list = await MapToDtosAsync([entity], ct);
        return list.FirstOrDefault();
    }

    private async Task<List<WalletWithdrawalRequestDto>> MapToDtosAsync(
        List<WalletWithdrawalRequest> entities,
        CancellationToken ct)
    {
        if (entities.Count == 0)
            return [];

        var userIds = entities
            .Select(w => w.UserId)
            .Distinct()
            .ToList();

        var userNames = await context.Set<Domain.User.Aggregates.User>()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new
            {
                Id = u.Id.Value,
                u.FullName.FirstName,
                u.FullName.LastName
            })
            .ToListAsync(ct);

        var namesById = userNames.ToDictionary(
            x => x.Id,
            x => $"{x.FirstName} {x.LastName}".Trim());

        return entities.Select(w => new WalletWithdrawalRequestDto(
            w.Id.Value,
            w.UserId.Value,
            namesById.TryGetValue(w.UserId.Value, out var name) ? name : null,
            w.Amount.Amount,
            w.Iban.Value,
            w.AccountHolder,
            w.Description,
            w.Status.ToString(),
            w.RejectionReason,
            w.BankReferenceNumber,
            w.CreatedAt,
            w.ApprovedAt,
            w.RejectedAt,
            w.PaidAt,
            w.CancelledAt)).ToList();
    }
}