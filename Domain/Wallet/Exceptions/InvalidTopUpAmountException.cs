using SharedKernel.Localization;

namespace Domain.Wallet.Exceptions;

public sealed class InvalidTopUpAmountException(decimal providedAmount, decimal min) : DomainException(
        DomainErrorCodes.Wallet.InvalidTopUpAmount,
        $"Top-up amount ({providedAmount:N0}) is less than the minimum allowed ({min:N0}).",
        new Dictionary<string, object?>
{
["providedAmount"] = providedAmount,
["minimumAmount"] = min
})
{
}
