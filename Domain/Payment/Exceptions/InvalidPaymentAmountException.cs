namespace Domain.Payment.Exceptions;

public sealed class InvalidPaymentAmountException(decimal expectedAmount, decimal actualAmount) : DomainException($"مبلغ پرداخت نامعتبر است. مبلغ مورد انتظار: {expectedAmount:N0}، مبلغ دریافتی: {actualAmount:N0}")
{
    public decimal ExpectedAmount { get; } = expectedAmount;
    public decimal ActualAmount { get; } = actualAmount;
    public decimal Difference { get; } = Math.Abs(expectedAmount - actualAmount);

    public bool IsOverpayment() => ActualAmount > ExpectedAmount;

    public bool IsUnderpayment() => ActualAmount < ExpectedAmount;
}