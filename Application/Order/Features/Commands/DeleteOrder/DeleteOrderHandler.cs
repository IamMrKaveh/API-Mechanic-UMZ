using Domain.Common.Exceptions;
using Domain.Order.Interfaces;

namespace Application.Order.Features.Commands.DeleteOrder;

public class DeleteOrderHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<DeleteOrderCommand, ServiceResult>
{
    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAuditService _auditService = auditService;

    public async Task<ServiceResult> Handle(
        DeleteOrderCommand request,
        CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, ct);
        if (order == null)
            return ServiceResult.NotFound("سفارش یافت نشد.");

        try
        {
            order.Delete(request.UserId);
        }
        catch (DomainException ex)
        {
            return ServiceResult.Unexpected(ex.Message);
        }

        await _orderRepository.UpdateAsync(order, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditService.LogOrderEventAsync(
            order.Id,
            "DeleteOrder",
            request.UserId,
            $"سفارش {order.Id} حذف شد.");

        return ServiceResult.Success();
    }
}