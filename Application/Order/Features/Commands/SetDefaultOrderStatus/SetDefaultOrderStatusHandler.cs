using Application.Cache.Contracts;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;

namespace Application.Order.Features.Commands.SetDefaultOrderStatus;

public class SetDefaultOrderStatusHandler(
    IOrderStatusRepository orderStatusRepository)
    : ICommandHandler<SetDefaultOrderStatusCommand>
{
    public async Task<ServiceResult> Handle(
        SetDefaultOrderStatusCommand request,
        CancellationToken ct)
    {
        var statusId = OrderStatusId.From(request.Id);
        var statusResult = await (orderStatusRepository.GetByIdAsync(statusId, ct)).OrNotFoundAsync("وضعیت سفارش یافت نشد.");
        if (statusResult.IsFailure) return statusResult.Error;
        var status = statusResult.Value;

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


        return ServiceResult.Success();
    }
}