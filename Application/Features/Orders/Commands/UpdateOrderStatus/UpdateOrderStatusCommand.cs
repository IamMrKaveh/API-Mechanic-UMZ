namespace Application.Features.Orders.Commands.UpdateOrderStatus;

public record UpdateOrderStatusCommand(int OrderId, UpdateOrderStatusByIdDto Dto) : IRequest<ServiceResult>;