namespace Application.Wallet.Features.Commands.ReleaseWalletReservation;

public record ReleaseWalletReservationCommand(
    Guid UserId,
    Guid WalletReservationId) : IRequest<ServiceResult<Unit>>;