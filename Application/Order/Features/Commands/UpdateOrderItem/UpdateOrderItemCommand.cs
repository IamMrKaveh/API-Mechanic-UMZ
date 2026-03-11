using Application.Common.Models;

namespace Application.Order.Features.Commands.UpdateOrderItem;

public record UpdateOrderItemCommand(int Id, UpdateOrderItemDto Dto) : IRequest<ServiceResult>;