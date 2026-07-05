using Application.Cart.Features.Shared;

namespace Application.Cart.Features.Commands.UpdateCartItemQuantity;

public record UpdateCartItemQuantityCommand(
    Guid VariantId,
    int Quantity) : ICommand<CartDetailDto>;