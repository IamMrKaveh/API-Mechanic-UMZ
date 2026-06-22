namespace Application.Shipping.Features.Commands.RestoreShipping;

public record RestoreShippingCommand(
    Guid Id)
    : ICommand;