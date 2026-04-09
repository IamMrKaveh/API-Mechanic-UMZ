namespace Domain.Inventory.Exceptions;

public sealed class InvalidStockQuantityException : DomainException
{
    public int Quantity { get; }

    public override string ErrorCode => "INVALID_STOCK_QUANTITY";

    public InvalidStockQuantityException(int quantity)
        : base($"Stock quantity '{quantity}' is invalid. Quantity must be greater than zero.")
    {
        Quantity = quantity;
    }
}