namespace Application.Wallet.Features.Commands.RequestWithdrawal;

public sealed record RequestWithdrawalCommand(
    decimal Amount,
    string Iban,
    string AccountHolder,
    string? Description) : ICommand<Guid>;