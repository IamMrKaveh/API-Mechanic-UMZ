using Application.Common.Results;
using Application.Discount.Features.Shared;
using Domain.Common.Interfaces;
using Domain.Common.ValueObjects;
using Domain.Discount.Aggregates;
using Domain.Discount.Interfaces;
using Domain.Discount.ValueObjects;
using Mapster;

namespace Application.Discount.Features.Commands.CreateDiscount;

public class CreateDiscountHandler(
    IDiscountRepository discountRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IRequestHandler<CreateDiscountCommand, ServiceResult<DiscountDto>>
{
    public async Task<ServiceResult<DiscountDto>> Handle(CreateDiscountCommand request, CancellationToken ct)
    {
        var discountValue = DiscountValue.Create(request.DiscountType, request.DiscountValue);
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