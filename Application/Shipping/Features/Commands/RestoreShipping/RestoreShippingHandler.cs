using Domain.Shipping.Interfaces;
using Domain.Shipping.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Shipping.Features.Commands.RestoreShipping;

public class RestoreShippingHandler(
    IShippingRepository shippingMethodRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<RestoreShippingCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        RestoreShippingCommand request,
        CancellationToken ct)
    {
        var shippingId = ShippingId.From(request.Id);
        var adminId = UserId.From(request.CurrentUserId);

        var shipping = await shippingMethodRepository.GetByIdAsync(shippingId, ct);
        if (shipping is null)
            return ServiceResult.NotFound("روش ارسال یافت نشد.");

        shipping.Restore();

        shippingMethodRepository.Update(shipping);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogAdminEventAsync(
            "RestoreShippingMethod",
            adminId,
            $"Restored shipping method ID: {request.Id}");

        return ServiceResult.Success();
    }
}