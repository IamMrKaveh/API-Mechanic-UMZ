using Application.Cache.Contracts;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;

namespace Application.Order.Features.Commands.DeleteOrderStatus;

public class DeleteOrderStatusHandler(
    IOrderStatusRepository orderStatusRepository)
    : ICommandHandler<DeleteOrderStatusCommand>
{
    public async Task<ServiceResult> Handle(DeleteOrderStatusCommand request, CancellationToken ct)
    {
        var statusId = OrderStatusId.From(request.Id);
        var statusResult = await (orderStatusRepository.GetByIdAsync(statusId, ct)).OrNotFoundAsync("وضعیت سفارش یافت نشد.");
        if (statusResult.IsFailure) return statusResult.Error;
        var status = statusResult.Value;

        if (status.IsDefault)
            return ServiceResult.Forbidden("امکان حذف وضعیت پیش‌فرض وجود ندارد.");

        var isUsed = await orderStatusRepository.IsInUseAsync(statusId, ct);
        if (isUsed)
            return ServiceResult.Forbidden("امکان حذف وضعیتی که به سفارشات اختصاص داده شده وجود ندارد.");

        status.MarkAsDeleted();
        orderStatusRepository.Remove(status);


        return ServiceResult.Success();
    }
}