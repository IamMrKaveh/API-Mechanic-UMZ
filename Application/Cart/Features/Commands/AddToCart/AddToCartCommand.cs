namespace Application.Cart.Features.Commands.AddToCart;

public record AddToCartCommand(
    int VariantId,
    int Quantity
    ) : IRequest<ServiceResult<CartDetailDto>>;