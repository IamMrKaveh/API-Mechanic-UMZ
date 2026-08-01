using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;

namespace Application.Wallet.Features.Commands.CreditWallet;

public class CreditWalletHandler(
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    IDistributedLock distributedLock,
    ICurrentUserService currentUserService)
    : ICommandHandler<CreditWalletCommand, Unit>
{
    private const string DefaultCurrency = "IRT";
    private static readonly TimeSpan WalletLockExpiry = TimeSpan.FromSeconds(10);

    public async Task<ServiceResult<Unit>> Handle(CreditWalletCommand request, CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);

        await using var lockHandle = await distributedLock.AcquireAsync(
            $"wallet:{userId.Value:N}",
            WalletLockExpiry,
            ct);

        if (lockHandle is null || !lockHandle.IsAcquired)
            return ServiceResult<Unit>.Conflict("عملیات دیگری روی کیف پول در حال انجام است. لطفاً مجدداً تلاش کنید.");

        try
        {
            var alreadyProcessed = await walletRepository.HasIdempotencyKeyAsync(userId, request.IdempotencyKey, ct);
            if (alreadyProcessed)
                return ServiceResult<Unit>.Success(Unit.Value);

            var wallet = await walletRepository.GetByUserIdForUpdateAsync(userId, ct);
            if (wallet is null)
            {
                wallet = Domain.Wallet.Aggregates.Wallet.Create(userId);
                await walletRepository.AddAsync(wallet, ct);
            }

            if (wallet.IsActive is false && currentUserService.IsAdmin)
            {
                var adminId = UserId.From(currentUserService.UserId.Value);
                wallet.Unfreeze(adminId);
            }

            var amount = Money.Create(request.Amount, DefaultCurrency);

            wallet.Credit(
                amount,
                request.Description ?? request.TransactionType.ToString(),
                request.ReferenceId,
                request.IdempotencyKey);

            walletRepository.Update(wallet);
            await unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (ConcurrencyException)
        {
            return ServiceResult<Unit>.Conflict("تعارض همزمانی رخ داد. لطفاً مجدداً تلاش کنید.");
        }
        catch (DomainException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
    }
}
