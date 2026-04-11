using Application.Discount.Features.Shared;

namespace Application.Discount.Features.Queries.ValidateDiscount;

public record ValidateDiscountQuery(
    string Code,
    decimal OrderAmount,
    string Currency,
    Guid UserId) : IRequest<ServiceResult<DiscountValidationResult>>;