using System.Text;
using System.Text.Json;
using Application.Wallet.Contracts;
using Application.Wallet.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.Wallet.Features.Queries.ExportWalletLedger;

public sealed class ExportWalletLedgerHandler(
    IWalletQueryService walletQueryService)
    : IQueryHandler<ExportWalletLedgerQuery, ExportWalletLedgerResult>
{
    public async Task<ServiceResult<ExportWalletLedgerResult>> Handle(
        ExportWalletLedgerQuery request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);

        var filter = new WalletLedgerFilter
        {
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            TransactionType = request.TransactionType,
            MinAmount = request.MinAmount,
            MaxAmount = request.MaxAmount,
            SearchTerm = request.SearchTerm,
            MaxRows = request.MaxRows
        };

        var entries = await walletQueryService.ExportLedgerAsync(userId, filter, ct);

        var isJson = string.Equals(request.Format, "json", StringComparison.OrdinalIgnoreCase);
        var extension = isJson ? "json" : "csv";
        var contentType = isJson ? "application/json" : "text/csv";

        var content = isJson
            ? JsonSerializer.SerializeToUtf8Bytes(entries, new JsonSerializerOptions { WriteIndented = true })
            : BuildCsv(entries);

        var fileName = $"wallet_ledger_{request.UserId:N}_{DateTime.UtcNow:yyyyMMdd_HHmm}.{extension}";
        return ServiceResult<ExportWalletLedgerResult>.Success(new ExportWalletLedgerResult(content, fileName, contentType));
    }

    private static byte[] BuildCsv(IReadOnlyList<WalletLedgerEntryDto> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,WalletId,UserId,AmountDelta,BalanceAfter,TransactionType,ReferenceType,ReferenceId,Description,CreatedAt,IsAdminAdjustment");

        foreach (var e in entries)
        {
            sb.Append(e.Id).Append(',');
            sb.Append(e.WalletId).Append(',');
            sb.Append(e.UserId).Append(',');
            sb.Append(e.AmountDelta.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(',');
            sb.Append(e.BalanceAfter.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(',');
            sb.Append(Escape(e.TransactionType)).Append(',');
            sb.Append(Escape(e.ReferenceType)).Append(',');
            sb.Append(e.ReferenceId).Append(',');
            sb.Append(Escape(e.Description)).Append(',');
            sb.Append(e.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")).Append(',');
            sb.Append(e.IsAdminAdjustment ? "true" : "false");
            sb.AppendLine();
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var needsQuote = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        return needsQuote ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
    }
}