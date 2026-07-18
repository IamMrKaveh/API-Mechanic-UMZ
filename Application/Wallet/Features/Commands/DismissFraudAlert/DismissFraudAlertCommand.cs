namespace Application.Wallet.Features.Commands.DismissFraudAlert;

public sealed record DismissFraudAlertCommand(
    Guid AlertId,
    string? Note) : ICommand<Unit>;