using Application.Cart.Features.Shared;

namespace Application.Cart.Features.Queries.GetCart;

public record GetCartQuery(
    Guid? UserId,
    string? GuestToken) : IRequest<ServiceResult<CartDetailDto>>;