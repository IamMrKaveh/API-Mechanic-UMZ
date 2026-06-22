namespace Application.Order.Features.Commands.DeleteOrderStatus;

public record DeleteOrderStatusCommand(
    Guid Id)
    : ICommand;