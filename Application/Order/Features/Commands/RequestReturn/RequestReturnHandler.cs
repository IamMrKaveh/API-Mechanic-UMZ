using Application.Audit.Contracts;
using Application.Common.Exceptions;
using Application.Common.Results;
using Application.Notification.Contracts;
using Domain.Common.Exceptions;
using Domain.Common.Interfaces;
using Domain.Order.Interfaces;

namespace Application.Order.Features.Commands.RequestReturn;

public class RequestReturnHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    INotificationService notificationService,
    IAuditService auditService,
    ILogger<RequestReturnHandler> logger) : IRequestHandler<RequestReturnCommand, ServiceResult>
{
    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly INotificationService _notificationService = notificationService;
    private readonly IAuditService _auditService = auditService;
    private readonly ILogger<RequestReturnHandler> _logger = logger;

    public async Task<ServiceResult> Handle(
        RequestReturnCommand request,
        CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(request.OrderId, ct);
        if (order == null)
            return ServiceResult.NotFound("سفارش یافت نشد.");

        if (order.UserId != request.UserId)
            return ServiceResult.Unauthorized("شما مجاز به درخواست بازگشت این سفارش نیستید.");

        if (!string.IsNullOrEmpty(request.RowVersion))
            _orderRepository.SetOriginalRowVersion(order, Convert.FromBase64String(request.RowVersion));

        var oldStatusName = order.Status.DisplayName;

        try
        {
            order.MarkAsReturned(request.Reason);
        }
        catch (DomainException ex)
        {
            return ServiceResult.Unexpected(ex.Message);
        }

        await _orderRepository.UpdateAsync(order, ct);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);

            await _notificationService.SendOrderStatusNotificationAsync(
                order.UserId,
                order.Id,
                oldStatusName,
                OrderStatusValue.Returned.DisplayName);

            await _auditService.LogOrderEventAsync(
                order.Id,
                "RequestReturn",
                request.UserId,
                $"درخواست بازگشت سفارش. دلیل: {request.Reason}");

            _logger.LogInformation(
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