using Application.Common.Results;

namespace Application.Discount.Features.Queries.ValidateDiscount;

public record ValidateDiscountQuery(
    string Code,
    decimal OrderTotal,
    int UserId) : IRequest<ServiceResult<DiscountValidationDto>>;