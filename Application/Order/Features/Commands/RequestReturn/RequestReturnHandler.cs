using Domain.Common.Exceptions;
using Domain.Common.ValueObjects;
using Domain.Order.Interfaces;

namespace Application.Order.Features.Commands.RequestReturn;

public class RequestReturnHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    INotificationService notificationService,
    IAuditService auditService,
    ILogger<RequestReturnHandler> logger) : IRequestHandler<RequestReturnCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        RequestReturnCommand request,
        CancellationToken ct)
    {
        var order = await orderRepository.GetByIdWithItemsAsync(request.OrderId, ct);
        if (order is null)
            return ServiceResult.NotFound("سفارش یافت نشد.");

        if (order.UserId != request.UserId)
            return ServiceResult.Unauthorized("شما مجاز به درخواست بازگشت این سفارش نیستید.");

        if (!string.IsNullOrEmpty(request.RowVersion))
            orderRepository.SetOriginalRowVersion(order, Convert.FromBase64String(request.RowVersion));

        var oldStatusName = order.Status.DisplayName;

        try
        {
            order.MarkAsReturned(request.Reason);
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }

        await orderRepository.UpdateAsync(order, ct);

        try
        {
            await unitOfWork.SaveChangesAsync(ct);

            await notificationService.SendOrderStatusNotificationAsync(
                order.UserId,
                order.Id,
                oldStatusName,
                OrderStatusValue.Returned.DisplayName);

            await auditService.LogOrderEventAsync(
                order.Id,
                "RequestReturn",
                IpAddress.Unknown,
                request.UserId,
                $"درخواست بازگشت سفارش. دلیل: {request.Reason}");

            logger.LogInformation(
                "Order {OrderId} return requested by user {UserId}. Reason: {Reason}",
                order.Id, request.UserId, request.Reason);

            return ServiceResult.Success();
        }
        catch (ConcurrencyException)
        {
            return ServiceResult.Conflict("این سفارش توسط کاربر دیگری تغییر کرده است. لطفاً صفحه را رفرش کنید.");
        }
    }
}