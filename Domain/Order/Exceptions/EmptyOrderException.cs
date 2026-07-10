namespace Domain.Order.Exceptions;

public sealed class EmptyOrderException : DomainException
{
    public override string ErrorCode => "EMPTY_ORDER";

    public EmptyOrderException()
        : base("An order must contain at least one item.")
    {
    }
}