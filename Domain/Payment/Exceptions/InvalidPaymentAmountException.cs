namespace Domain.Payment.Exceptions;

public sealed class InvalidPaymentAmountException(decimal expectedAmount, decimal actualAmount) : DomainException($"مبلغ پرداخت نامعتبر است. مبلغ مورد انتظار: {expectedAmount:N0}، مبلغ دریافتی: {actualAmount:N0}")
{
    public decimal ExpectedAmount { get; } = expectedAmount;
    public decimal ActualAmount { get; } = actualAmount;
    public decimal Difference => Math.Abs(ExpectedAmount - ActualAmount);

    public override string ErrorCode => "INVALID_PAYMENT_AMOUNT";
}