namespace Application.Shipping.Features.Commands.DeleteShipping;

public record DeleteShippingCommand(
    Guid Id)
    : ICommand;