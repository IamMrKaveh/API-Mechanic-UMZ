using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Application.Wallet.Features.Commands.ReleaseWalletReservation;

public class ReleaseWalletReservationHandler(
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<ReleaseWalletReservationCommand, ServiceResult<Unit>>
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

            wallet.ReleaseReservation(reservationId);
            walletRepository.Update(wallet);
            await unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (ConcurrencyException)
        {
            await auditService.LogSystemEventAsync(
                "WalletReleaseConcurrencyConflict",
                $"تعارض همزمانی در آزادسازی رزرو کیف پول {request.WalletReservationId}");
            return ServiceResult<Unit>.Conflict("تعارض همزمانی رخ داد. لطفاً مجدداً تلاش کنید.");
        }
        catch (Exception)
        {
            return ServiceResult<Unit>.Failure("خطا در آزادسازی رزرو کیف پول.");
        }
    }
}