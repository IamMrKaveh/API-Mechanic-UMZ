using Application.Cache.Contracts;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;

namespace Application.Order.Features.Commands.DeactivateOrderStatus;

public class DeactivateOrderStatusHandler(
    IOrderStatusRepository orderStatusRepository)
    : ICommandHandler<DeactivateOrderStatusCommand>
{
    public async Task<ServiceResult> Handle(
        DeactivateOrderStatusCommand request,
        CancellationToken ct)
    {
        var statusId = OrderStatusId.From(request.Id);
        var statusResult = await (orderStatusRepository.GetByIdAsync(statusId, ct)).OrNotFoundAsync("وضعیت سفارش یافت نشد.");
        if (statusResult.IsFailure) return statusResult.Error;
        var status = statusResult.Value;

        if (!status.IsActive)
            return ServiceResult.Success();

        try
        {
            status.Deactivate();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }

        orderStatusRepository.Update(status);


        return ServiceResult.Success();
    }
}