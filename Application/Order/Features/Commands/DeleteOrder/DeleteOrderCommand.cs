namespace Application.Order.Features.Commands.DeleteOrder;

public record DeleteOrderCommand(
    Guid OrderId)
    : ICommand;