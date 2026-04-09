using Application.Wallet.Features.Shared;

namespace Application.Wallet.Contracts;

public interface IWalletQueryService
{
    Task<PaginatedResult<WalletLedgerEntryDto>> GetLedgerPageAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<WalletLedgerEntryDto?> GetOrderPaymentLedgerEntryAsync(
        int userId,
        int orderId,
        CancellationToken ct = default);
}