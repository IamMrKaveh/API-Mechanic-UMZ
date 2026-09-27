using Application.Discount.Features.Shared;
using Domain.Discount.Enums;
using Domain.Discount.Interfaces;
using Domain.Discount.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Discount.Features.Commands.UpdateDiscount;

public class UpdateDiscountHandler(
    IDiscountRepository discountRepository,
    IMapper mapper,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<UpdateDiscountCommand, DiscountDto>
{
    public async Task<ServiceResult<DiscountDto>> Handle(UpdateDiscountCommand request, CancellationToken ct)
    {
        var discountResult = await (discountRepository.GetByIdAsync(DiscountCodeId.From(request.Id), ct)).OrNotFoundAsync("کد تخفیف یافت نشد.");
        if (discountResult.IsFailure) return discountResult.Error;
        var discount = discountResult.Value;

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

        var now = dateTimeProvider.UtcNow;
        discount.Update(discountValue, maxDiscount, request.UsageLimit, request.StartsAt, request.ExpiresAt, now);

        if (request.IsActive && !discount.IsActive)
            discount.Activate(now);
        else if (!request.IsActive && discount.IsActive)
            discount.Deactivate(now);

        discountRepository.Update(discount);

        return ServiceResult<DiscountDto>.Success(mapper.Map<DiscountDto>(discount));
    }
}