namespace Application.Order.Features.Commands.UpdateOrderStatus;

public record UpdateOrderStatusCommand(
    int OrderId,
    string NewStatus,
    string RowVersion,
    int UpdatedByUserId) : IRequest<ServiceResult>;