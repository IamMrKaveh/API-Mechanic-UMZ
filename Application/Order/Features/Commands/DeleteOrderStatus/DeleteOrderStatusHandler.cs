using Application.Audit.Contracts;
using Application.Cache.Contracts;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;

namespace Application.Order.Features.Commands.DeleteOrderStatus;

public class DeleteOrderStatusHandler(
    IOrderStatusRepository orderStatusRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICacheService cacheService)
    : ICommandHandler<DeleteOrderStatusCommand>
{
    public async Task<ServiceResult> Handle(DeleteOrderStatusCommand request, CancellationToken ct)
    {
        var statusId = OrderStatusId.From(request.Id);
        var status = await orderStatusRepository.GetByIdAsync(statusId, ct);
        if (status is null)
            return ServiceResult.NotFound("وضعیت سفارش یافت نشد.");

        if (status.IsDefault)
            return ServiceResult.Forbidden("امکان حذف وضعیت پیش‌فرض وجود ندارد.");

        var isUsed = await orderStatusRepository.IsInUseAsync(statusId, ct);
        if (isUsed)
            return ServiceResult.Forbidden("امکان حذف وضعیتی که به سفارشات اختصاص داده شده وجود ندارد.");

        status.MarkAsDeleted();
        orderStatusRepository.Remove(status);
        await unitOfWork.SaveChangesAsync(ct);

        await cacheService.RemoveByPrefixAsync("order-status:", ct);

        await auditService.LogSystemEventAsync(
            "OrderStatusDeleted",
            $"وضعیت سفارش {status.Name} حذف شد.",
            ct);

        return ServiceResult.Success();
    }
}