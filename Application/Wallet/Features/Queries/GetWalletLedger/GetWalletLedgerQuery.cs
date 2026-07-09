using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Queries.GetWalletLedger;

public record GetWalletLedgerQuery(
    Guid UserId,
    int Page = 1,
    int PageSize = 10,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? TransactionType = null,
    decimal? MinAmount = null,
    decimal? MaxAmount = null,
    string? SearchTerm = null)
    : IPageQuery<WalletLedgerEntryDto>;