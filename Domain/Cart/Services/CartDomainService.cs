using Domain.Cart.Enum;

namespace Domain.Cart.Services;

public sealed class CartDomainService
{
    public void MergeCarts(
        Aggregates.Cart targetCart,
        Aggregates.Cart sourceCart,
        CartMergeStrategy strategy = CartMergeStrategy.SumQuantities)
    {
        ArgumentNullException.ThrowIfNull(targetCart);
        ArgumentNullException.ThrowIfNull(sourceCart);

        targetCart.MergeFrom(sourceCart, strategy);
    }
}