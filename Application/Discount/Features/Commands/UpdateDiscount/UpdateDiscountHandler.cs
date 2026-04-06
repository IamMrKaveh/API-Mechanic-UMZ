using Application.Common.Results;
using Application.Discount.Features.Shared;
using Domain.Discount.Interfaces;
using Domain.Discount.ValueObjects;
using Domain.Common.Interfaces;

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

        if (request.IsActive && !discount.IsActive)
            discount.Activate();
        else if (!request.IsActive && discount.IsActive)
            discount.Deactivate();

        discountRepository.Update(discount);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult<DiscountDto>.Success(mapper.Map<DiscountDto>(discount));
    }
}