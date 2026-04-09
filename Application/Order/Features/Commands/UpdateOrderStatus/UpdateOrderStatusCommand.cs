using Application.Common.Results;

namespace Application.Order.Features.Commands.UpdateOrderStatus;

public record UpdateOrderStatusCommand(
    Guid OrderId,
    Guid OrderStatusId,
    string RowVersion,
    Guid UpdatedByUserId) : IRequest<ServiceResult>;