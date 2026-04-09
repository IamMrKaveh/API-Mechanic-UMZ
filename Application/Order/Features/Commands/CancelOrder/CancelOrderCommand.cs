namespace Application.Order.Features.Commands.CancelOrder;

public record CancelOrderCommand(
    Guid OrderId,
    Guid UserId,
    string Reason,
    bool IsAdmin = false) : IRequest<ServiceResult>;