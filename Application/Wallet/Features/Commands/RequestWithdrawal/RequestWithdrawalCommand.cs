namespace Application.Wallet.Features.Commands.RequestWithdrawal;

public sealed record RequestWithdrawalCommand(
    Guid UserId,
    decimal Amount,
    string Iban,
    string AccountHolder,
    string? Description) : ICommand<Guid>;