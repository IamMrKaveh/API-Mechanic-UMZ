namespace Domain.Order.Results;

public sealed class OrderStatusTransitionValidation
{
    public bool IsValid { get; private set; }
    public string? Error { get; private set; }

    private OrderStatusTransitionValidation() { }

    public static OrderStatusTransitionValidation Success()
    {
        return new OrderStatusTransitionValidation { IsValid = true };
    }

    public static OrderStatusTransitionValidation Failed(string error)
    {
        return new OrderStatusTransitionValidation { IsValid = false, Error = error };
    }
}
