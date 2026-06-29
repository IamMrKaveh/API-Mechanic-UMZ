namespace Application.Order.Features.Commands.SetDefaultOrderStatus;

public record SetDefaultOrderStatusCommand(
    Guid Id)
    : ICommand;