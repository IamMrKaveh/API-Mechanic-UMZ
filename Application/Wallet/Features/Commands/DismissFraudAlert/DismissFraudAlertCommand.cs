namespace Application.Wallet.Features.Commands.DismissFraudAlert;

public sealed record DismissFraudAlertCommand(
    Guid AlertId,
    Guid AdminId,
    string? Note) : ICommand<Unit>;