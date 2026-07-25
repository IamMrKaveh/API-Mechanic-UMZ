using Application.Discount.Features.Shared;
using Domain.Discount.Aggregates;
using Domain.Discount.Enums;
using Domain.Discount.Exceptions;
using Domain.Discount.Interfaces;
using Domain.Discount.ValueObjects;

namespace Application.Discount.Features.Commands.CreateDiscount;

public class CreateDiscountHandler(
    IDiscountRepository discountRepository,
    IMapper mapper)
    : ICommandHandler<CreateDiscountCommand, DiscountDto>
{
    public async Task<ServiceResult<DiscountDto>> Handle(CreateDiscountCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return ServiceResult<DiscountDto>.Failure("کد تخفیف الزامی است.");

        var existing = await discountRepository.GetByCodeAsync(request.Code, ct);
        if (existing is not null)
            return ServiceResult<DiscountDto>.Conflict("کد تخفیف تکراری است. لطفاً کد دیگری وارد کنید.");

        DiscountValue discountValue;
        try
        {
            discountValue = request.DiscountType switch
            {
                DiscountType.Percentage => DiscountValue.Percentage(request.Value),
                DiscountType.FixedAmount => DiscountValue.Fixed(request.Value),
                DiscountType.FreeShipping => DiscountValue.FreeShipping(),
                _ => throw new DomainException("نوع تخفیف نامعتبر است.")
            };
        }
        catch (DomainException ex)
        {
            return ServiceResult<DiscountDto>.Failure(ex.Message);
        }

        Money? maxDiscount = request.MaximumDiscountAmount.HasValue
            ? Money.FromDecimal(request.MaximumDiscountAmount.Value)
            : null;

        DiscountCode discount;
        try
        {
            discount = DiscountCode.Create(
                DiscountCodeId.NewId(),
                request.Code,
                discountValue,
                maxDiscount,
                request.UsageLimit,
                request.StartsAt,
                request.ExpiresAt);
        }
        catch (InvalidDiscountException ex)
        {
            return ServiceResult<DiscountDto>.Failure(ex.Message);
        }
        catch (DomainException ex)
        {
            return ServiceResult<DiscountDto>.Failure(ex.Message);
        }

        await discountRepository.AddAsync(discount, ct);

        return ServiceResult<DiscountDto>.Success(mapper.Map<DiscountDto>(discount));
    }
}
