namespace Application.Wallet.Features.Commands.ReserveWallet;

public record ReserveWalletCommand(
    int UserId,
    decimal Amount,
    int OrderId,
    DateTime? ExpiresAt = null
) : IRequest<ServiceResult<Unit>>;