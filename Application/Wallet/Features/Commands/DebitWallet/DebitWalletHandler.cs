using Domain.User.ValueObjects;
using Domain.Wallet.Exceptions;
using Domain.Wallet.Interfaces;

namespace Application.Wallet.Features.Commands.DebitWallet;

public class DebitWalletHandler(
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<DebitWalletCommand, ServiceResult<Unit>>
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
                request.Description ?? request.TransactionType.ToString(),
                request.ReferenceId);

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
                "WalletDebitConcurrencyConflict",
                $"تعارض همزمانی در برداشت از کیف پول. IdempotencyKey: {request.IdempotencyKey}");
            return ServiceResult<Unit>.Conflict("تعارض همزمانی رخ داد. لطفاً مجدداً تلاش کنید.");
        }
        catch (DomainException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
        catch (Exception)
        {
            return ServiceResult<Unit>.Failure("خطا در برداشت از کیف پول.");
        }
    }
}