namespace Application.Order.Features.Commands.DeleteOrderStatus;

public record DeleteOrderStatusCommand(int Id, int DeletedByUserId) : IRequest<ServiceResult>;