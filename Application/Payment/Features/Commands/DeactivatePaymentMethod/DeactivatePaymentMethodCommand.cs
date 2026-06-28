namespace Application.Payment.Features.Commands.DeactivatePaymentMethod;

public record DeactivatePaymentMethodCommand(Guid Id) : ICommand;