namespace Application.Discount.Features.Commands.DeleteDiscount;

public record DeleteDiscountCommand(Guid Id) : IRequest<ServiceResult>;