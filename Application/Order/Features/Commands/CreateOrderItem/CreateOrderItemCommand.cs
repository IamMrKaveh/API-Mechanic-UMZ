namespace Application.Order.Features.Commands.CreateOrderItem;

public record CreateOrderItemCommand(int OrderId, CreateOrderItemDto Dto) : IRequest<ServiceResult>;