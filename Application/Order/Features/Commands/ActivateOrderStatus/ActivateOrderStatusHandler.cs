using Application.Audit.Contracts;
using Application.Cache.Contracts;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;

namespace Application.Order.Features.Commands.ActivateOrderStatus;

public class ActivateOrderStatusHandler(
    IOrderStatusRepository orderStatusRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICacheService cacheService)
    : ICommandHandler<ActivateOrderStatusCommand>
{
    public async Task<ServiceResult> Handle(
        ActivateOrderStatusCommand request,
        CancellationToken ct)
    {
        var statusId = OrderStatusId.From(request.Id);
        var status = await orderStatusRepository.GetByIdAsync(statusId, ct);
        if (status is null)
            return ServiceResult.NotFound("وضعیت سفارش یافت نشد.");

        if (status.IsActive)
            return ServiceResult.Success();

        try
        {
            status.Activate();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }

        orderStatusRepository.Update(status);
        await unitOfWork.SaveChangesAsync(ct);

        await cacheService.RemoveByPrefixAsync("order-status:", ct);

        await auditService.LogSystemEventAsync(
            "OrderStatusActivated",
            $"وضعیت سفارش {status.Name} فعال شد.",
            ct);

        return ServiceResult.Success();
    }
}