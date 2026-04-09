using Application.Shipping.Features.Shared;
using Domain.Common.ValueObjects;
using Domain.Shipping.Interfaces;
using Domain.Shipping.ValueObjects;

namespace Application.Shipping.Features.Commands.UpdateShipping;

public class UpdateShippingHandler(
    IShippingRepository shippingRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IRequestHandler<UpdateShippingCommand, ServiceResult<ShippingDto>>
{
    public async Task<ServiceResult<ShippingDto>> Handle(UpdateShippingCommand request, CancellationToken ct)
    {
        var shipping = await shippingRepository.GetByIdAsync(ShippingId.From(request.Id), ct);
        if (shipping is null)
            return ServiceResult<ShippingDto>.NotFound("روش ارسال یافت نشد.");

        if (await shippingRepository.ExistsByNameAsync(request.Name, request.Id, ct))
            return ServiceResult<ShippingDto>.Conflict("روش ارسال با این نام قبلاً ثبت شده است.");

        shipping.Update(
            ShippingName.Create(request.Name),
            Money.FromDecimal(request.BaseCost),
            request.Description,
            request.EstimatedDeliveryTime,
            request.MinDeliveryDays,
            request.MaxDeliveryDays);

        shippingRepository.Update(shipping);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult<ShippingDto>.Success(mapper.Map<ShippingDto>(shipping));
    }
}