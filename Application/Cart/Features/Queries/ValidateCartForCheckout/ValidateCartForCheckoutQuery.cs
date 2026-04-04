using Application.Common.Results;

namespace Application.Cart.Features.Queries.ValidateCartForCheckout;

public record ValidateCartForCheckoutQuery : IRequest<ServiceResult<CartCheckoutValidationDto>>;