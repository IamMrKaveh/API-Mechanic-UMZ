using Application.Common.Interfaces;
using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Commands.InitiateWalletTransfer;

public sealed record InitiateWalletTransferCommand(
    Guid FromUserId,
    string RecipientPhoneNumber,
    decimal Amount,
    string? Description) : ICommand<InitiateWalletTransferResultDto>;