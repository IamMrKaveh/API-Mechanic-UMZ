namespace Application.Cart.Features.Commands.AddToCart;

public record AddToCartCommand(
    Guid? UserId,
    Guid VariantId,
    string? GuestToken,
    int Quantity) : IRequest<ServiceResult>;