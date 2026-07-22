namespace Application.Order.Features.Commands.ConfirmDelivery;

public record ConfirmDeliveryCommand(
    Guid OrderId,
    string? RowVersion)
    : ICommand;
