using Application.Common.Models;

namespace Application.Order.Features.Commands.ConfirmDelivery;

public class ConfirmDeliveryHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    INotificationService notificationService,
    IAuditService auditService,
    ILogger<ConfirmDeliveryHandler> logger) : IRequestHandler<ConfirmDeliveryCommand, ServiceResult>
{
    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly INotificationService _notificationService = notificationService;
    private readonly IAuditService _auditService = auditService;
    private readonly ILogger<ConfirmDeliveryHandler> _logger = logger;

    public async Task<ServiceResult> Handle(
        ConfirmDeliveryCommand request,
        CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, ct);
        if (order == null)
            return ServiceResult.Failure("سفارش یافت نشد.", 404);

        if (order.UserId != request.UserId)
            return ServiceResult.Failure("شما مجاز به تأیید تحویل این سفارش نیستید.", 403);

        if (!string.IsNullOrEmpty(request.RowVersion))
            _orderRepository.SetOriginalRowVersion(order, Convert.FromBase64String(request.RowVersion));

        var oldStatusName = order.Status.DisplayName;

        try
        {
            order.MarkAsDelivered();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message, 400);
        }

        await _orderRepository.UpdateAsync(order, ct);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);

            await _notificationService.SendOrderStatusNotificationAsync(
                order.UserId,
                order.Id,
                oldStatusName,
                OrderStatusValue.Delivered.DisplayName);

            await _auditService.LogOrderEventAsync(
                order.Id,
                "ConfirmDelivery",
                request.UserId,
                "تحویل سفارش توسط کاربر تأیید شد.");

            _logger.LogInformation("Order {OrderId} delivery confirmed by user {UserId}", order.Id, request.UserId);

            return ServiceResult.Success();
        }
        catch (ConcurrencyException)
        {
            return ServiceResult.Failure(
                "این سفارش توسط کاربر دیگری تغییر کرده است. لطفاً صفحه را رفرش کنید.", 409);
        }
    }
}