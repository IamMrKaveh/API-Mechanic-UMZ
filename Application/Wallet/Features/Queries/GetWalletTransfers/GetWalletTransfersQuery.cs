using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Queries.GetWalletTransfers;

public sealed record GetWalletTransfersQuery(
    Guid? UserId = null,
    string? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int Page = 1,
    int PageSize = 20)
    : IPageQuery<WalletTransferDto>;
