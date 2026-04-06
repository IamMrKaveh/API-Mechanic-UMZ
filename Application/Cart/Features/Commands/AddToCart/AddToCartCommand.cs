using Application.Cart.Features.Shared;
using Application.Common.Results;

namespace Application.Cart.Features.Commands.AddToCart;

public record AddToCartCommand(
    int? UserId,
    string? GuestToken,
    int VariantId,
    int Quantity) : IRequest<ServiceResult<CartDetailDto>>;