using Application.Common.Results;
using Domain.Discount.Interfaces;
using Domain.Discount.ValueObjects;
using Domain.Common.Interfaces;

namespace Application.Discount.Features.Commands.DeleteDiscount;

public class DeleteDiscountHandler(
    IDiscountRepository discountRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteDiscountCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(DeleteDiscountCommand request, CancellationToken ct)
    {
        var discount = await discountRepository.GetByIdAsync(DiscountCodeId.From(request.Id), ct);
        if (discount is null)
            return ServiceResult.NotFound("کد تخفیف یافت نشد.");

        discount.Deactivate();
        discountRepository.Update(discount);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}