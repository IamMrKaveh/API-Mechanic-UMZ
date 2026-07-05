using Domain.Cart.Enum;

namespace Application.Cart.Features.Commands.MergeGuestCart;

public record MergeGuestCartCommand(
    CartMergeStrategy Strategy = CartMergeStrategy.SumQuantities) : ICommand;