namespace Application.Order.Features.Commands.DeactivateOrderStatus;

public record DeactivateOrderStatusCommand(
    Guid Id)
    : ICommand;