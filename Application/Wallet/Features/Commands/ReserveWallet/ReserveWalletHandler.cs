using Domain.Common.Exceptions;
using Domain.Common.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wallet.Exceptions;
using Domain.Wallet.Interfaces;

namespace Application.Wallet.Features.Commands.ReserveWallet;

public class ReserveWalletHandler(
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    ILogger<ReserveWalletHandler> logger) : IRequestHandler<ReserveWalletCommand, ServiceResult<Unit>>
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

            wallet.Reserve(
                Money.FromDecimal(request.Amount),
                request.WalletId,
                request.ExpiresAt);

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
            logger.LogWarning(
                "Concurrency conflict reserving wallet for user {UserId}. Retry recommended.",
                request.UserId);
            return ServiceResult<Unit>.Conflict("تعارض همزمانی رخ داد. لطفاً مجدداً تلاش کنید.");
        }
        catch (DomainException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reserving wallet for user {UserId}", userId);
            return ServiceResult<Unit>.Failure("خطا در رزرو کیف پول.");
        }
    }
}