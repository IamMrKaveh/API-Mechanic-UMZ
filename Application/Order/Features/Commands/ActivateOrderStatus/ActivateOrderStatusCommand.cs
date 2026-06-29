namespace Application.Order.Features.Commands.ActivateOrderStatus;

public record ActivateOrderStatusCommand(
    Guid Id)
    : ICommand;