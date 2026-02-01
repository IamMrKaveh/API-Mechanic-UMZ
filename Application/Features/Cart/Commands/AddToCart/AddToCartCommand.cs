namespace Application.Features.Cart.Commands.AddToCart;

public record AddToCartCommand(int? UserId, string? GuestId, AddToCartDto Dto) : IRequest<ServiceResult<CartDto>>;