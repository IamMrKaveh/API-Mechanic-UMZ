using Application.Common.Interfaces;
using MediatR;

namespace Application.Wallet.Features.Commands.CancelWalletTransfer;

public sealed record CancelWalletTransferCommand(
    Guid TransferId,
    Guid FromUserId) : ICommand<Unit>;