using Application.Order.Features.Shared;
using Domain.Order.ValueObjects;

namespace Application.Order.Features.Commands.UpdateOrder;

public record UpdateOrderCommand(OrderId OrderId, UpdateOrderDto Dto) : IRequest<ServiceResult>;