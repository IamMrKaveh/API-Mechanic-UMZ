namespace Domain.Order.Results;

public sealed class OrderCancellationValidation
{
    public bool CanCancel { get; private set; }
    public string? Error { get; private set; }
    public bool RequiresRefund { get; private set; }

    private OrderCancellationValidation()
    { }

    public static OrderCancellationValidation Success(bool requiresRefund)
    {
        return new OrderCancellationValidation
        {
            CanCancel = true,
            RequiresRefund = requiresRefund
        };
    }

    public static OrderCancellationValidation Failed(string error)
    {
        return new OrderCancellationValidation
        {
            CanCancel = false,
            Error = error
        };
    }
}