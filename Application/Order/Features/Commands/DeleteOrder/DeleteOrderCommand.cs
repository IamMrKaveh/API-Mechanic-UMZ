namespace Application.Features.Orders.Commands.DeleteOrder;

public record DeleteOrderCommand(int OrderId, int UserId) : IRequest<ServiceResult>;