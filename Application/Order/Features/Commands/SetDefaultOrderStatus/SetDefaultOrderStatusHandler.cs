using Application.Audit.Contracts;
using Application.Cache.Contracts;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;

namespace Application.Order.Features.Commands.SetDefaultOrderStatus;

public class SetDefaultOrderStatusHandler(
    IOrderStatusRepository orderStatusRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICacheService cacheService)
    : ICommandHandler<SetDefaultOrderStatusCommand>
{
    public async Task<ServiceResult> Handle(
        SetDefaultOrderStatusCommand request,
        CancellationToken ct)
    {
        var statusId = OrderStatusId.From(request.Id);
        var status = await orderStatusRepository.GetByIdAsync(statusId, ct);
        if (status is null)
            return ServiceResult.NotFound("وضعیت سفارش یافت نشد.");

        if (!status.IsActive)
            return ServiceResult.Validation("وضعیت غیرفعال نمی‌تواند پیش‌فرض شود.");

        if (status.IsDefault)
            return ServiceResult.Success();

        var currentDefault = await orderStatusRepository.GetDefaultAsync(ct);
        if (currentDefault is not null && currentDefault.Id != status.Id)
        {
            currentDefault.UnsetAsDefault();
            orderStatusRepository.Update(currentDefault);
        }

        try
        {
            status.SetAsDefault();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }

        orderStatusRepository.Update(status);
        await unitOfWork.SaveChangesAsync(ct);

        await cacheService.RemoveByPrefixAsync("order-status:", ct);

        await auditService.LogSystemEventAsync(
            "OrderStatusDefaultChanged",
            $"وضعیت پیش‌فرض سفارش به {status.Name} تغییر کرد.",
            ct);

        return ServiceResult.Success();
    }
}