using Application.Discount.Features.Shared;
using Domain.Common.ValueObjects;
using Domain.Discount.Enums;
using Domain.Discount.Interfaces;
using Domain.Discount.ValueObjects;

namespace Application.Discount.Features.Commands.UpdateDiscount;

public class UpdateDiscountHandler(
    IDiscountRepository discountRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IRequestHandler<UpdateDiscountCommand, ServiceResult<DiscountDto>>
{
    public async Task<ServiceResult<DiscountDto>> Handle(UpdateDiscountCommand request, CancellationToken ct)
    {
        var discount = await discountRepository.GetByIdAsync(DiscountCodeId.From(request.Id), ct);
        if (discount is null)
            return ServiceResult<DiscountDto>.NotFound("کد تخفیف یافت نشد.");

        DiscountValue discountValue = request.DiscountType switch
        {
            DiscountType.Percentage => DiscountValue.Percentage(request.Value),
            DiscountType.FixedAmount => DiscountValue.Fixed(request.Value),
            DiscountType.FreeShipping => DiscountValue.FreeShipping(),
            _ => throw new ArgumentOutOfRangeException(nameof(request.DiscountType))
        };

        Money? maxDiscount = request.MaximumDiscountAmount.HasValue
            ? Money.FromDecimal(request.MaximumDiscountAmount.Value)
            : null;

        discount.Update(discountValue, maxDiscount, request.UsageLimit, request.StartsAt, request.ExpiresAt);

        if (request.IsActive && !discount.IsActive)
            discount.Activate();
        else if (!request.IsActive && discount.IsActive)
            discount.Deactivate();

        discountRepository.Update(discount);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult<DiscountDto>.Success(mapper.Map<DiscountDto>(discount));
    }
}