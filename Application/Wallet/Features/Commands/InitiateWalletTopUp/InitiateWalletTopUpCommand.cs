using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Commands.InitiateWalletTopUp;

public sealed record InitiateWalletTopUpCommand(
    decimal Amount,
    string Gateway) : ICommand<InitiateTopUpResultDto>, IBypassTransactionBehavior;