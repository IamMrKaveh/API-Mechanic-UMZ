using Application.Common.Results;
using Application.Discount.Features.Shared;

namespace Application.Discount.Features.Queries.ValidateDiscount;

public record ValidateDiscountQuery(
    string Code,
    decimal OrderAmount,
    int UserId) : IRequest<ServiceResult<DiscountValidationResult>>;