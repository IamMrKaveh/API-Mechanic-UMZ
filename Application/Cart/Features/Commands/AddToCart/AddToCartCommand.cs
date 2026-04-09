namespace Application.Cart.Features.Commands.AddToCart;

public record AddToCartCommand(
    Guid? UserId,
    string? GuestToken,
    Guid VariantId,
    int Quantity) : IRequest<ServiceResult>;