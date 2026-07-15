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

        var shipping = await shippingMethodRepository.GetByIdAsync(shippingId, ct);
        if (shipping is null)
            return ServiceResult.NotFound("روش ارسال یافت نشد.");

        shipping.Restore();

        shippingMethodRepository.Update(shipping);

        await auditService.LogAdminEventAsync(
            "RestoreShippingMethod",
            adminId,
            $"Restored shipping method ID: {request.Id}");

        return ServiceResult.Success();
    }
}