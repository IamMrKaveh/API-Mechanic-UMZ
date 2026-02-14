using Application.Audit.Contracts;
using Application.Features.Orders.Commands.DeleteOrder;

namespace Application.Order.Features.Commands.DeleteOrder;

public class DeleteOrderHandler : IRequestHandler<DeleteOrderCommand, ServiceResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;

    public DeleteOrderHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
    }

    public async Task<ServiceResult> Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order == null)
            return ServiceResult.Failure("سفارش یافت نشد.", 404);

        try
        {
            // Use domain method which enforces business rules
            order.Delete(request.UserId);
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message, 400);
        }

        _orderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogOrderEventAsync(
            order.Id,
            "DeleteOrder",
            request.UserId,
            $"سفارش {order.Id} حذف نرم شد.");

        return ServiceResult.Success();
    }
}