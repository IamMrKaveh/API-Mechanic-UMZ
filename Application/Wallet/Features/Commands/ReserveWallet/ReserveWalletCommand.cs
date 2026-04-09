namespace Application.Wallet.Features.Commands.ReserveWallet;

public record ReserveWalletCommand(
    Guid UserId,
    decimal Amount,
    Guid WalletId,
    DateTime? ExpiresAt = null) : IRequest<ServiceResult<Unit>>;