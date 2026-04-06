using Application.Cart.Features.Shared;
using Application.Common.Results;

namespace Application.Cart.Features.Commands.RemoveFromCart;

public record RemoveFromCartCommand(
    int? UserId,
    string? GuestToken,
    int VariantId) : IRequest<ServiceResult<CartDetailDto>>;