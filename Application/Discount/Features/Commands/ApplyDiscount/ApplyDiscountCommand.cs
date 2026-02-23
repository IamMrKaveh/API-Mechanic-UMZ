namespace Application.Discount.Features.Commands.ApplyDiscount;

public record ApplyDiscountCommand(
    string Code,
    decimal OrderTotal,
    int UserId
    ) : IRequest<ServiceResult<DiscountApplyResultDto>>;