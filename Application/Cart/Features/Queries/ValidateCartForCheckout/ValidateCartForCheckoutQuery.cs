using Application.Cart.Features.Shared;

namespace Application.Cart.Features.Queries.ValidateCartForCheckout;

public record ValidateCartForCheckoutQuery(
    Guid? UserId,
    string? GuestToken) : IRequest<ServiceResult<CartCheckoutValidationDto>>;