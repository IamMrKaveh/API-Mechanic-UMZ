using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Application.Wallet.Features.Commands.ReleaseWalletReservation;

public class ReleaseWalletReservationHandler(
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    ILogger<ReleaseWalletReservationHandler> logger) : IRequestHandler<ReleaseWalletReservationCommand, ServiceResult<Unit>>
{
    public async Task<ServiceResult<Unit>> Handle(
        ReleaseWalletReservationCommand request,
        CancellationToken ct)
    {
        try
        {
            var userId = UserId.From(request.UserId);

            var walletReservationId = WalletReservationId.From(request.WalletReservationId);

            var wallet = await walletRepository.GetByUserIdForUpdateAsync(userId, ct);
            if (wallet is null)
                return ServiceResult<Unit>.Success(Unit.Value);

            wallet.ReleaseReservation(walletReservationId);
            walletRepository.Update(wallet);
            await unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (ConcurrencyException)
        {
            logger.LogWarning(
                "Concurrency conflict releasing wallet reservation for order {OrderId}.",
                request.WalletReservationId);
            return ServiceResult<Unit>.Conflict("تعارض همزمانی رخ داد. لطفاً مجدداً تلاش کنید.");
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error releasing wallet reservation for order {OrderId}",
                request.WalletReservationId);
            return ServiceResult<Unit>.Failure("خطا در آزادسازی رزرو کیف پول.");
        }
    }
}