using Domain.Cart.Enum;

namespace Application.Cart.Features.Commands.MergeGuestCart;

public record MergeGuestCartCommand(
    Guid? UserId,
    string? GuestToken,
    CartMergeStrategy Strategy = CartMergeStrategy.SumQuantities) : IRequest<ServiceResult>;