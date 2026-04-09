namespace Application.Cart.Features.Queries.ValidateCartForCheckout;

public record ValidateCartForCheckoutQuery : IRequest<ServiceResult<CartCheckoutValidationDto>>;