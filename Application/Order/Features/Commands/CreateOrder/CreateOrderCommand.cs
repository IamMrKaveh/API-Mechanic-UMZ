namespace Application.Order.Features.Commands.CreateOrder;

public record CreateOrderCommand(AdminCreateOrderDto Dto, string IdempotencyKey, int AdminUserId) : IRequest<ServiceResult<int>>;