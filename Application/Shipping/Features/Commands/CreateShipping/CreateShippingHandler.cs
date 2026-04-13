using Application.Shipping.Features.Shared;
using Domain.Shipping.Interfaces;
using Domain.Shipping.ValueObjects;

namespace Application.Shipping.Features.Commands.CreateShipping;

public class CreateShippingHandler(
    IShippingRepository shippingRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IRequestHandler<CreateShippingCommand, ServiceResult<ShippingDto>>
{
    public async Task<ServiceResult<ShippingDto>> Handle(CreateShippingCommand request, CancellationToken ct)
    {
        var shippingName = ShippingName.Create(request.Name);

        if (await shippingRepository.ExistsByNameAsync(shippingName, null, ct))
            return ServiceResult<ShippingDto>.Conflict("روش ارسال با این نام قبلاً ثبت شده است.");

        var shipping = Domain.Shipping.Aggregates.Shipping.Create(
            ShippingName.Create(request.Name),
            Money.FromDecimal(request.BaseCost),
            request.Description,
            request.EstimatedDeliveryTime,
            request.MinDeliveryDays,
            request.MaxDeliveryDays);

        await shippingRepository.AddAsync(shipping, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult<ShippingDto>.Success(mapper.Map<ShippingDto>(shipping));
    }
}