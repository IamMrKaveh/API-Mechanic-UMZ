namespace Application.Payment.Features.Commands.ActivatePaymentMethod;

public record ActivatePaymentMethodCommand(Guid Id) : ICommand;