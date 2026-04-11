using Application.Wallet.Features.Commands.CreditWallet;
using Domain.Common.Exceptions;
using Domain.Common.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wallet.Exceptions;
using Domain.Wallet.Interfaces;

namespace Application.Wallet.Features.Commands.DebitWallet;

public class DebitWalletHandler(
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    ILogger<DebitWalletHandler> logger) : IRequestHandler<DebitWalletCommand, ServiceResult<Unit>>
{
    public async Task<ServiceResult<Unit>> Handle(
        DebitWalletCommand request,
        CancellationToken ct)
    {
        try
        {
            var userId = UserId.From(request.UserId);

            var alreadyProcessed = await walletRepository.HasIdempotencyKeyAsync(userId, request.IdempotencyKey, ct);
            if (alreadyProcessed)
                return ServiceResult<Unit>.Success(Unit.Value);

            var wallet = await walletRepository.GetByUserIdForUpdateAsync(userId, ct);
            if (wallet is null)
                return ServiceResult<Unit>.NotFound("کیف پول یافت نشد.");

            wallet.Debit(
                Money.FromDecimal(request.Amount),
                request.Description,
                request.ReferenceId);

            walletRepository.Update(wallet);

            await unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (DbUpdateException)
        {
            logger.LogWarning(
                "Duplicate idempotency key (DB constraint) on debit: {Key} for user {UserId}",
                request.IdempotencyKey, request.UserId);
            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (InsufficientWalletBalanceException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
        catch (ConcurrencyException)
        {
            logger.LogWarning(
                "Concurrency conflict debiting wallet for user {UserId}. Retry recommended.",
                request.UserId);
            return ServiceResult<Unit>.Conflict("تعارض همزمانی رخ داد. لطفاً مجدداً تلاش کنید.");
        }
        catch (DomainException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error debiting wallet for user {UserId}", request.UserId);
            return ServiceResult<Unit>.Failure("خطا در برداشت از کیف پول.");
        }
    }
}