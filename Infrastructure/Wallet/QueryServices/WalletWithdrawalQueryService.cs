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

        var items = await Project(baseQuery)
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

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
            baseQuery = baseQuery.Where(w => w.Status == status.Value);

        var total = await baseQuery.CountAsync(ct);

        var items = await Project(baseQuery)
            .OrderBy(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return PaginatedResult<WalletWithdrawalRequestDto>.Create(items, total, page, pageSize);
    }

    public async Task<WalletWithdrawalRequestDto?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        return await Project(
            context.Set<WalletWithdrawalRequest>()
                .AsNoTracking()
                .Where(w => w.Id.Value == id))
            .FirstOrDefaultAsync(ct);
    }

    private IQueryable<WalletWithdrawalRequestDto> Project(IQueryable<WalletWithdrawalRequest> source)
    {
        return from w in source
               join u in context.Set<Domain.User.Aggregates.User>().AsNoTracking()
                   on w.UserId equals u.Id into userJoin
               from user in userJoin.DefaultIfEmpty()
               select new WalletWithdrawalRequestDto(
                   w.Id.Value,
                   w.UserId.Value,
                   user != null ? $"{user.FullName.FirstName} {user.FullName.LastName}" : null,
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
                   w.CancelledAt);
    }
}