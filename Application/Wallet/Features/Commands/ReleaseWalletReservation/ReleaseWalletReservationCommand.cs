using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Application.Wallet.Features.Commands.ReleaseWalletReservation;

public record ReleaseWalletReservationCommand(
    UserId UserId,
    WalletReservationId WalletReservationId) : IRequest<ServiceResult<Unit>>;