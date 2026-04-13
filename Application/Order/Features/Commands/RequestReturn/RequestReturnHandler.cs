using Domain.Common.Exceptions;
using Domain.Common.ValueObjects;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Order.Features.Commands.RequestReturn;

public class RequestReturnHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    INotificationService notificationService,
    IAuditService auditService) : IRequestHandler<RequestReturnCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        RequestReturnCommand request,
        CancellationToken ct)
    {
        var orderId = OrderId.From(request.OrderId);
        var order = await orderRepository.FindWithItemsByIdAsync(orderId, ct);
        if (order is null)
            return ServiceResult.NotFound("سفارش یافت نشد.");

        if (order.UserId != UserId.From(request.UserId))
            return ServiceResult.Unauthorized("شما مجاز به درخواست بازگشت این سفارش نیستید.");

        if (!string.IsNullOrEmpty(request.RowVersion))
            orderRepository.SetOriginalRowVersion(order, Convert.FromBase64String(request.RowVersion));

        var oldStatusName = order.Status.DisplayName;

        try
        {
            order.MarkAsReturned();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }

        orderRepository.Update(order);

        try
        {
            await unitOfWork.SaveChangesAsync(ct);

            await notificationService.SendOrderStatusNotificationAsync(
                order.UserId,
                order.Id,
                oldStatusName,
                OrderStatusValue.Returned.DisplayName,
                ct);

            await auditService.LogOrderEventAsync(
                order.Id,
                "RequestReturn",
                IpAddress.Unknown,
                UserId.From(request.UserId),
                $"درخواست بازگشت سفارش. دلیل: {request.Reason}",
                ct);

            return ServiceResult.Success();
        }
        catch (ConcurrencyException)
        {
            return ServiceResult.Conflict("این سفارش توسط کاربر دیگری تغییر کرده است.");
        }
    }
}