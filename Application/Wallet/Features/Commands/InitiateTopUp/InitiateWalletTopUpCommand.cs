using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Commands.InitiateTopUp;

public sealed record InitiateWalletTopUpCommand(
    Guid UserId,
    decimal Amount,
    string Gateway) : ICommand<InitiateTopUpResultDto>;