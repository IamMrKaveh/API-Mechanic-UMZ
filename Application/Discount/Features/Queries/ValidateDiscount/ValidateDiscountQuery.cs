using Application.Common.Models;

namespace Application.Discount.Features.Queries.ValidateDiscount;

public record ValidateDiscountQuery(
    string Code,
    decimal OrderTotal,
    int UserId) : IRequest<ServiceResult<DiscountValidationDto>>;