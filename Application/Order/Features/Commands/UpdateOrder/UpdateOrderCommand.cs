using Application.Order.Features.Shared;

namespace Application.Order.Features.Commands.UpdateOrder;

public record UpdateOrderCommand(Guid OrderId, UpdateOrderDto Dto) : IRequest<ServiceResult>;