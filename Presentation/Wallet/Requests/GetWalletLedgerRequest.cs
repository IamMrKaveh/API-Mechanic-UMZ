namespace Presentation.Wallet.Requests;

public sealed record GetAdminWalletLedgerRequest(
    int Page = 1,
    int PageSize = 10,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? TransactionType = null,
    decimal? MinAmount = null,
    decimal? MaxAmount = null,
    string? SearchTerm = null);

public sealed record ExportAdminWalletLedgerRequest(
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? TransactionType = null,
    decimal? MinAmount = null,
    decimal? MaxAmount = null,
    string? SearchTerm = null,
    string Format = "csv",
    int? MaxRows = null);