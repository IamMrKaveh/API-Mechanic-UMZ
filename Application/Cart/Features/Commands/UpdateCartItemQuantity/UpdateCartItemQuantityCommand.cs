using Application.Common.Models;

namespace Application.Cart.Features.Commands.UpdateCartItemQuantity;

public record UpdateCartItemQuantityCommand(
    int VariantId,
    int Quantity
    ) : IRequest<ServiceResult<CartDetailDto>>;