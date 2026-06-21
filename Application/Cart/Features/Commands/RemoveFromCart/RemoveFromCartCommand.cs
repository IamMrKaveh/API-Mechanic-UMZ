using Application.Cart.Features.Shared;

namespace Application.Cart.Features.Commands.RemoveFromCart;

public record RemoveFromCartCommand(
    Guid? UserId,
    string? GuestToken,
    Guid VariantId) : ICommand<CartDetailDto>;