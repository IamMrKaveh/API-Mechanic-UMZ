using Application.Discount.Features.Shared;

namespace Application.Discount.Features.Queries.ValidateDiscount;

public record ValidateDiscountQuery(
    string Code,
    decimal OrderAmount,
    Guid UserId,
    string Currency) : IRequest<ServiceResult<DiscountValidationResult>>;