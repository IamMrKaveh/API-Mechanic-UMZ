namespace Application.Order.Features.Commands.RequestReturn;

public sealed record RequestReturnCommand(
    Guid OrderId,
    Guid UserId,
    string Reason,
    string RowVersion) : IRequest<ServiceResult>;