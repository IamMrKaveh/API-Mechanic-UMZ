using Domain.User.ValueObjects;
using Domain.Wallet.Exceptions;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Application.Wallet.Features.Commands.ReserveWallet;

public class ReserveWalletHandler(
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<ReserveWalletCommand, ServiceResult<Unit>>
{
    public async Task<ServiceResult<Unit>> Handle(
        ReserveWalletCommand request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);

        try
        {
            var wallet = await walletRepository.GetByUserIdForUpdateAsync(userId, ct);
            if (wallet is null)
                return ServiceResult<Unit>.NotFound("کیف پول یافت نشد.");

            var reservationId = WalletReservationId.NewId();

            wallet.CreateReservation(
                reservationId,
                Money.FromDecimal(request.Amount),
                $"reservation-{request.WalletId}");

            walletRepository.Update(wallet);
            await unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (InsufficientWalletBalanceException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
        catch (ConcurrencyException)
        {
            await auditService.LogSystemEventAsync(
                "WalletReserveConcurrencyConflict",
                $"تعارض همزمانی در رزرو کیف پول کاربر {userId.Value}",
                ct);
            return ServiceResult<Unit>.Conflict("تعارض همزمانی رخ داد. لطفاً مجدداً تلاش کنید.");
        }
        catch (DomainException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
        catch (Exception)
        {
            return ServiceResult<Unit>.Failure("خطا در رزرو کیف پول.");
        }
    }
}