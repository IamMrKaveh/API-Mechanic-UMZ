using Application.Common.Results;
using Application.Discount.Features.Shared;

namespace Application.Discount.Features.Commands.ApplyDiscount;

public record ApplyDiscountCommand(
    string Code,
    decimal OrderAmount,
    Guid UserId,
    string OrderId) : IRequest<ServiceResult<DiscountApplicationResultDto>>;