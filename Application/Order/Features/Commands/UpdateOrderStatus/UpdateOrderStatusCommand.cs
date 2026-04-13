namespace Application.Order.Features.Commands.UpdateOrderStatus;

public record UpdateOrderStatusCommand(
    Guid OrderId,
    string NewStatus,
    string RowVersion,
    Guid UpdatedByUserId) : IRequest<ServiceResult>;