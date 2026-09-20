using Domain.Discount.Interfaces;
using Domain.Discount.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Discount.Features.Commands.DeleteDiscount;

public class DeleteDiscountHandler(
    IDiscountRepository discountRepository,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<DeleteDiscountCommand>
{
    public async Task<ServiceResult> Handle(DeleteDiscountCommand request, CancellationToken ct)
    {
        var discount = await discountRepository.GetByIdAsync(DiscountCodeId.From(request.Id), ct);
        if (discount is null)
            return ServiceResult.NotFound("کد تخفیف یافت نشد.");

        discount.Deactivate(dateTimeProvider.UtcNow);
        discountRepository.Update(discount);

        return ServiceResult.Success();
    }
}