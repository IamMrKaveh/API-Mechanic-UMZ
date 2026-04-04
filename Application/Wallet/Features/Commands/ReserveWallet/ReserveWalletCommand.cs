using Application.Common.Results;
using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Application.Wallet.Features.Commands.ReserveWallet;

public record ReserveWalletCommand(
    UserId UserId,
    decimal Amount,
    WalletId WalletId,
    DateTime? ExpiresAt = null) : IRequest<ServiceResult<Unit>>;