using Application.Common.Results;

namespace Application.Order.Features.Commands.DeleteOrder;

public record DeleteOrderCommand(Guid OrderId, Guid UserId) : IRequest<ServiceResult>;