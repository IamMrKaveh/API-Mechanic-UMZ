namespace Application.Payment.Features.Commands.DeletePaymentMethod;

public record DeletePaymentMethodCommand(Guid Id) : ICommand;