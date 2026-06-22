namespace Application.Payment.Features.Commands.ExpireStalePayments;

public record ExpireStalePaymentsCommand(
    DateTime CutOff)
    : ICommand<int>;