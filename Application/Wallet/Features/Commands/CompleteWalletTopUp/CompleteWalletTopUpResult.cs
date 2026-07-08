namespace Application.Wallet.Features.Commands.CompleteWalletTopUp;

public sealed record CompleteWalletTopUpResult(
    Guid? TopUpId,
    bool IsSuccess,
    string StatusText,
    string? Message,
    decimal? Amount = null,
    string? RefId = null);