namespace Domain.Cart.Exceptions;

public class InvalidCartOperationException : DomainException
{
    public int CartId { get; }
    public string Operation { get; }
    public string Reason { get; }

    public InvalidCartOperationException(int cartId, string operation, string reason)
        : base($"عملیات '{operation}' روی سبد خرید {cartId} امکان‌پذیر نیست. دلیل: {reason}")
    {
        CartId = cartId;
        Operation = operation;
        Reason = reason;
    }
}