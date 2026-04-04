using Application.Common.Results;

namespace Application.Cart.Features.Commands.RemoveFromCart;

public record RemoveFromCartCommand(
    int VariantId
    ) : IRequest<ServiceResult<CartDetailDto>>;