namespace Domain.Inventory.Exceptions;

public sealed class InvalidStockQuantityException(int quantity) : Exception($"Stock quantity '{quantity}' is invalid. Quantity must be greater than zero.")
{
}