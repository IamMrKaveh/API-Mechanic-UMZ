using Application.Audit.Contracts;

namespace Application.Order.Features.Commands.RequestReturn;

public class RequestReturnHandler : IRequestHandler<RequestReturnCommand, ServiceResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly IAuditService _auditService;
    private readonly ILogger<RequestReturnHandler> _logger;

    public RequestReturnHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        IAuditService auditService,
        ILogger<RequestReturnHandler> logger)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(
        RequestReturnCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(request.OrderId, cancellationToken);
        if (order == null)
            return ServiceResult.Failure("سفارش یافت نشد.", 404);

        if (order.UserId != request.UserId)
            return ServiceResult.Failure("شما مجاز به درخواست بازگشت این سفارش نیستید.", 403);

        if (!string.IsNullOrEmpty(request.RowVersion))
            _orderRepository.SetOriginalRowVersion(order, Convert.FromBase64String(request.RowVersion));

        var oldStatusName = order.Status.DisplayName;

        try
        {
            order.MarkAsReturned(request.Reason);
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message, 400);
        }

        _orderRepository.Update(order);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);

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
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult.Failure(
                "این سفارش توسط کاربر دیگری تغییر کرده است. لطفاً صفحه را رفرش کنید.", 409);
        }
    }
}