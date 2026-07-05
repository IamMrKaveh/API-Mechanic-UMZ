using Application.Cart.Features.Shared;

namespace Application.Cart.Features.Commands.RemoveItemFromCart;

public record RemoveItemFromCartCommand(
    Guid VariantId) : ICommand<CartDetailDto>;