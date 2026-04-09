namespace Application.Order.Features.Commands.UpdateOrder;

public record UpdateOrderCommand(Guid OrderId, Guid Dto) : IRequest<ServiceResult>;