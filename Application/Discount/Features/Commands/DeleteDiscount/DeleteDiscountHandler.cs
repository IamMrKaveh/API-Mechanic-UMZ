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
        var discountResult = await (discountRepository.GetByIdAsync(DiscountCodeId.From(request.Id), ct)).OrNotFoundAsync("کد تخفیف یافت نشد.");
        if (discountResult.IsFailure) return discountResult.Error;
        var discount = discountResult.Value;

        discount.Deactivate(dateTimeProvider.UtcNow);
        discountRepository.Update(discount);

        return ServiceResult.Success();
    }
}