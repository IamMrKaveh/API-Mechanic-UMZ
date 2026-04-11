namespace Application.Order.Features.Commands.DeleteOrderStatus;

public record DeleteOrderStatusCommand(Guid Id, Guid DeletedByUserId) : IRequest<ServiceResult>;