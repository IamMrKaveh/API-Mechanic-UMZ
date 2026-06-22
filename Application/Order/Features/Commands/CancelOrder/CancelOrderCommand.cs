namespace Application.Order.Features.Commands.CancelOrder;

public record CancelOrderCommand(
    Guid OrderId,
    string Reason)
    : ICommand;