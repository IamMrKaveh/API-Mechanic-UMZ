using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Queries.ExportWalletLedger;

public sealed record ExportWalletLedgerQuery(
    Guid UserId,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? TransactionType = null,
    decimal? MinAmount = null,
    decimal? MaxAmount = null,
    string? SearchTerm = null,
    string Format = "csv",
    int MaxRows = 10_000) : IQuery<ExportWalletLedgerResult>;