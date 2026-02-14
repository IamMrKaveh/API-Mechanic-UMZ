namespace Domain.Order.Exceptions;

public sealed class InvalidOrderStateException : DomainException
{
    public int OrderId { get; }
    public string CurrentState { get; }
    public string AttemptedOperation { get; }

    public InvalidOrderStateException(int orderId, string currentState, string attemptedOperation)
        : base($"عملیات '{attemptedOperation}' برای سفارش {orderId} در وضعیت '{currentState}' امکان‌پذیر نیست.")
    {
        OrderId = orderId;
        CurrentState = currentState;
        AttemptedOperation = attemptedOperation;
    }
}