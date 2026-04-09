using Application.Common.Results;

namespace Application.Order.Features.Commands.DeleteOrderItem;

public record DeleteOrderItemCommand(Guid Id) : IRequest<ServiceResult>;