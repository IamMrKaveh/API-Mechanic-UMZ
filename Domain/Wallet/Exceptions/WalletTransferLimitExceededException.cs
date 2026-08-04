using SharedKernel.Localization;

namespace Domain.Wallet.Exceptions;

public sealed class WalletTransferLimitExceededException(decimal dailyLimit, decimal alreadyTransferredToday) : DomainException(
        DomainErrorCodes.Wallet.TransferLimitExceeded,
        $"Daily transfer limit ({dailyLimit:N0}) has been reached. Total transferred today: {alreadyTransferredToday:N0}.",
        new Dictionary<string, object?>
{
["dailyLimit"] = dailyLimit,
["alreadyTransferredToday"] = alreadyTransferredToday
})
{
}
