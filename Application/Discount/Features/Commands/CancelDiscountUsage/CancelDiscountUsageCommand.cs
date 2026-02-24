namespace Application.Discount.Features.Commands.CancelDiscountUsage;

public record CancelDiscountUsageCommand(
    int OrderId,
    int DiscountCodeId
    ) : IRequest<ServiceResult>;