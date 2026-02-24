namespace Domain.Cart.Enum;

public enum CartMergeStrategy
{
    KeepHigherQuantity,
    SumQuantities,
    KeepUserCart,
    KeepGuestCart
}