using Application.Common.Results;

namespace Application.Cart.Features.Commands.UpdateCartItemQuantity;

public record UpdateCartItemQuantityCommand(
    int VariantId,
    int Quantity
    ) : IRequest<ServiceResult<CartDetailDto>>;