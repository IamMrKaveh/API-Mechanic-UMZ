using Domain.Shipping.Interfaces;
using Domain.Shipping.ValueObjects;

namespace Application.Shipping.Features.Commands.SetDefaultShipping;

public class SetDefaultShippingHandler(
    IShippingRepository shippingRepository)
    : ICommandHandler<SetDefaultShippingCommand>
{
    public async Task<ServiceResult> Handle(SetDefaultShippingCommand request, CancellationToken ct)
    {
        var shippingId = ShippingId.From(request.Id);

        var shippingResult = await (shippingRepository.GetByIdAsync(shippingId, ct)).OrNotFoundAsync("روش ارسال یافت نشد.");
        if (shippingResult.IsFailure) return shippingResult.Error;
        var shipping = shippingResult.Value;

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
            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }
}