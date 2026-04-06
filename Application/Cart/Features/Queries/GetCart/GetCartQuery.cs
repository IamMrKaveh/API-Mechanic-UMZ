using Application.Cart.Features.Shared;
using Application.Common.Results;

namespace Application.Cart.Features.Queries.GetCart;

public record GetCartQuery(int? UserId, string? GuestToken) : IRequest<ServiceResult<CartDetailDto>>;