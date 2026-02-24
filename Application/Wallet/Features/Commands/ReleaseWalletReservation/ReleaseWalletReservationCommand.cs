namespace Application.Wallet.Features.Commands.ReleaseWalletReservation;

public record ReleaseWalletReservationCommand(
    int UserId,
    int OrderId
    ) : IRequest<ServiceResult<Unit>>;