using Domain.Common.Exceptions;
using Domain.Shipping.Interfaces;
using Domain.Shipping.ValueObjects;

namespace Application.Shipping.Features.Commands.SetDefaultShipping;

public class SetDefaultShippingHandler(
    IShippingRepository shippingRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<SetDefaultShippingCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(SetDefaultShippingCommand request, CancellationToken ct)
    {
        var shippingId = ShippingId.From(request.Id);

        var shipping = await shippingRepository.GetByIdAsync(shippingId, ct);
        if (shipping is null)
            return ServiceResult.NotFound("روش ارسال یافت نشد.");

        var currentDefault = await shippingRepository.GetDefaultAsync(ct);
        if (currentDefault is not null && currentDefault.Id != shipping.Id)
        {
            currentDefault.UnsetDefault();
            shippingRepository.Update(currentDefault);
        }

        try
        {
            shipping.SetAsDefault();
            shippingRepository.Update(shipping);
            await unitOfWork.SaveChangesAsync(ct);
            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }
}