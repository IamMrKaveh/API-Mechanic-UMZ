namespace Application.Wallet.Features.Shared;

public sealed record ExportWalletLedgerResult(byte[] FileContent, string FileName, string ContentType);