using Application.Wallet.Features.Shared;

namespace Application.Wallet.Contracts;

public interface IWalletDebitRequestQueryService
{
    Task<PaginatedResult<AdminDebitRequestListItemDto>> GetPageAsync(
        int page,
        int pageSize,
        WalletDebitRequestFilter? filter,
        CancellationToken ct = default);
}
