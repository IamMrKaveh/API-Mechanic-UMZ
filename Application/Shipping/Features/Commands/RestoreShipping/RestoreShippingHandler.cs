using Domain.Shipping.Interfaces;
using Domain.Shipping.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Shipping.Features.Commands.RestoreShipping;

public class RestoreShippingHandler(
    IShippingRepository shippingMethodRepository,
    ICurrentUserService currentUser,
    IAuditService auditService)
    : ICommandHandler<RestoreShippingCommand>
{
    public async Task<ServiceResult> Handle(
        RestoreShippingCommand request,
        CancellationToken ct)
    {
        var shippingId = ShippingId.From(request.Id);
        var adminId = UserId.From(currentUser.UserId!.Value);

        var shippingResult = await (shippingMethodRepository.GetByIdAsync(shippingId, ct)).OrNotFoundAsync("روش ارسال یافت نشد.");
        if (shippingResult.IsFailure) return shippingResult.Error;
        var shipping = shippingResult.Value;

        shipping.Restore();

        shippingMethodRepository.Update(shipping);

        await auditService.LogAdminEventAsync(
            "RestoreShippingMethod",
            adminId,
            $"Restored shipping method ID: {request.Id}");


        return ServiceResult.Success();
    }
}