using Application.Wallet.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Wallet.Contracts;

public interface IWalletQueryService
{
    Task<PaginatedResult<WalletLedgerEntryDto>> GetLedgerPageAsync(
        UserId userId,
        int page,
        int pageSize,
        WalletLedgerFilter? filter = null,
        CancellationToken ct = default);

    Task<WalletLedgerEntryDto?> GetOrderPaymentLedgerEntryAsync(
        UserId userId,
        OrderId orderId,
        CancellationToken ct = default);

    Task<IReadOnlyList<WalletLedgerEntryDto>> ExportLedgerAsync(
        UserId userId,
        WalletLedgerFilter filter,
        CancellationToken ct = default);

    Task<PaginatedResult<WalletOverviewDto>> GetOverviewPageAsync(
        int page,
        int pageSize,
        WalletOverviewFilter? filter = null,
        CancellationToken ct = default);

    Task<WalletStatisticsDto> GetStatisticsAsync(CancellationToken ct = default);
}