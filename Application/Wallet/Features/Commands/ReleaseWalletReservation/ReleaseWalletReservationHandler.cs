using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Wallet.Features.Commands.ReleaseWalletReservation;

public class ReleaseWalletReservationHandler(
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService)
    : ICommandHandler<ReleaseWalletReservationCommand, Unit>
{
    public async Task<ServiceResult<Unit>> Handle(
        ReleaseWalletReservationCommand request,
        CancellationToken ct)
    {
        try
        {
            var userId = UserId.From(request.UserId);
            var reservationId = WalletReservationId.From(request.WalletReservationId);

            var wallet = await walletRepository.GetByUserIdForUpdateAsync(userId, ct);
            if (wallet is null)
                return ServiceResult<Unit>.Success(Unit.Value);

            wallet.ReleaseReservation(reservationId, dateTimeProvider.UtcNow);
            walletRepository.Update(wallet);
            await unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (ConcurrencyException)
        {
            await auditService.LogSystemEventAsync(
                "WalletReleaseConcurrencyConflict",
                $"تعارض همزمانی در آزادسازی رزرو کیف پول {request.WalletReservationId}",
                ct);
            return ServiceResult<Unit>.Conflict("تعارض همزمانی رخ داد. لطفاً مجدداً تلاش کنید.");
        }
        catch (DomainException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
    }
}