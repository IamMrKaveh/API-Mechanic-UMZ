using Application.Common.Models;

namespace Application.Features.Orders.Commands.UpdateOrder;

public record UpdateOrderCommand(int OrderId, UpdateOrderDto Dto) : IRequest<ServiceResult>;