using Application.Discount.Features.Shared;

namespace Application.Discount.Features.Commands.ApplyDiscount;

public record ApplyDiscountCommand(
    string Code,
    decimal OrderAmount,
    Guid UserId,
    Guid OrderId) : IRequest<ServiceResult<DiscountApplicationResult>>;