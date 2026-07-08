namespace Domain.Wallet.Exceptions;

public sealed class WalletTransferLimitExceededException(decimal dailyLimit, decimal alreadyTransferredToday) : DomainException($"سقف انتقال روزانه ({dailyLimit:N0} تومان) پر شده است. مجموع انتقال‌های امروز: {alreadyTransferredToday:N0} تومان.")
{
    public override string ErrorCode => "WALLET_TRANSFER_LIMIT_EXCEEDED";

    public decimal DailyLimit { get; } = dailyLimit;
    public decimal AlreadyTransferredToday { get; } = alreadyTransferredToday;
}