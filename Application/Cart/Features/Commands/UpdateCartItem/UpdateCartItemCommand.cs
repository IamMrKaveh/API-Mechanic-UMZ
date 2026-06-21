using Application.Cart.Features.Shared;

namespace Application.Cart.Features.Commands.UpdateCartItem;

public record UpdateCartItemCommand(
    Guid? UserId,
    string? GuestToken,
    Guid VariantId,
    int Quantity) : ICommand<CartDetailDto>;