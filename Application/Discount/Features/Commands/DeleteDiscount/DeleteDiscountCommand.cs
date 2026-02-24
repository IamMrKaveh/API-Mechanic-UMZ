namespace Application.Discount.Features.Commands.DeleteDiscount;

public record DeleteDiscountCommand(
    int Id
    ) : IRequest<ServiceResult>;