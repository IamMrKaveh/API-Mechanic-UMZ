namespace Application.Cart.Features.Commands.AddItemToCart;

public record AddItemToCartCommand(
    Guid VariantId,
    int Quantity) : ICommand;