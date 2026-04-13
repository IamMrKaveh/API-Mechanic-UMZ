using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;

namespace Application.Order.Features.Commands.DeleteOrderStatus;

public class DeleteOrderStatusHandler(
    IOrderStatusRepository orderStatusRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteOrderStatusCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(DeleteOrderStatusCommand request, CancellationToken ct)
    {
        var statusId = OrderStatusId.From(request.Id);
        var status = await orderStatusRepository.GetByIdAsync(statusId, ct);
        if (status is null)
            return ServiceResult.NotFound("وضعیت سفارش یافت نشد.");

        var isUsed = await orderStatusRepository.IsInUseAsync(statusId, ct);
        if (isUsed)
            return ServiceResult.Forbidden("امکان حذف وضعیتی که به سفارشات اختصاص داده شده وجود ندارد.");

        orderStatusRepository.Remove(status);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}