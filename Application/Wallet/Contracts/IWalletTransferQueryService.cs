using Application.Wallet.Features.Shared;

namespace Application.Wallet.Contracts;

public interface IWalletTransferQueryService
{
    Task<PaginatedResult<WalletTransferDto>> GetTransfersPageAsync(
        int page,
        int pageSize,
        WalletTransferFilter? filter,
        CancellationToken ct = default);

    Task<WalletTransferDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
