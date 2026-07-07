namespace Domain.Wallet.Exceptions;

public sealed class InvalidTopUpAmountException : DomainException
{
    public decimal ProvidedAmount { get; }
    public decimal Min { get; }

    public InvalidTopUpAmountException(decimal providedAmount, decimal min)
        : base($"مبلغ شارژ ({providedAmount:N0}) کمتر از حداقل مجاز ({min:N0} تومان) است.")
    {
        ProvidedAmount = providedAmount;
        Min = min;
    }
}