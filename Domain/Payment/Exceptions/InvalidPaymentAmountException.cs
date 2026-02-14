namespace Domain.Payment.Exceptions;

public sealed class InvalidPaymentAmountException : DomainException
{
    public decimal ExpectedAmount { get; }
    public decimal ActualAmount { get; }
    public decimal Difference { get; }

    public InvalidPaymentAmountException(decimal expectedAmount, decimal actualAmount)
        : base($"مبلغ پرداخت نامعتبر است. مبلغ مورد انتظار: {expectedAmount:N0}، مبلغ دریافتی: {actualAmount:N0}")
    {
        ExpectedAmount = expectedAmount;
        ActualAmount = actualAmount;
        Difference = Math.Abs(expectedAmount - actualAmount);
    }

    public bool IsOverpayment() => ActualAmount > ExpectedAmount;

    public bool IsUnderpayment() => ActualAmount < ExpectedAmount;
}