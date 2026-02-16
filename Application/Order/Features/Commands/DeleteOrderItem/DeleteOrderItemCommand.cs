namespace Application.Order.Features.Commands.DeleteOrderItem;

public record DeleteOrderItemCommand(int Id) : IRequest<ServiceResult>;