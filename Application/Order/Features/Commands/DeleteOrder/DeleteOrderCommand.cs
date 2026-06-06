namespace Application.Order.Features.Commands.DeleteOrder;

public record DeleteOrderCommand(Guid OrderId) : IRequest<ServiceResult>;