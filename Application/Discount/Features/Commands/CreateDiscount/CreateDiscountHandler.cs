using Application.Discount.Features.Shared;
using Domain.Discount.Aggregates;
using Domain.Discount.Enums;
using Domain.Discount.Interfaces;
using Domain.Discount.ValueObjects;

namespace Application.Discount.Features.Commands.CreateDiscount;

public class CreateDiscountHandler(
    IDiscountRepository discountRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IRequestHandler<CreateDiscountCommand, ServiceResult<DiscountDto>>
{
    public async Task<ServiceResult<DiscountDto>> Handle(CreateDiscountCommand request, CancellationToken ct)
    {
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

        var discount = DiscountCode.Create(
            DiscountCodeId.NewId(),
            request.Code,
            discountValue,
            maxDiscount,
            request.UsageLimit,
            request.StartsAt,
            request.ExpiresAt);

        await discountRepository.AddAsync(discount, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult<DiscountDto>.Success(mapper.Map<DiscountDto>(discount));
    }
}