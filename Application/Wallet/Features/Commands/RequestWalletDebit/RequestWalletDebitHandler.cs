using Domain.User.ValueObjects;
using Domain.Wallet.Exceptions;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Application.Wallet.Features.Commands.RequestWalletDebit;

public sealed class RequestWalletDebitHandler(
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    IDistributedLock distributedLock,
    ICurrentUserService currentUserService)
    : ICommandHandler<RequestWalletDebitCommand, Guid>
{
    private const string DefaultCurrency = "IRT";
    private static readonly TimeSpan WalletLockExpiry = TimeSpan.FromSeconds(10);

    public async Task<ServiceResult<Guid>> Handle(RequestWalletDebitCommand request, CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);
        var adminId = UserId.From(currentUserService.UserId!.Value);

        await using var lockHandle = await distributedLock.AcquireAsync(
            $"wallet:{userId.Value:N}",
            WalletLockExpiry,
            ct);

        if (lockHandle is null || !lockHandle.IsAcquired)
            return ServiceResult<Guid>.Conflict("عملیات دیگری روی کیف پول در حال انجام است.");

        try
        {
            var wallet = await walletRepository.GetByUserIdForUpdateAsync(userId, ct);
            if (wallet is null)
                return ServiceResult<Guid>.NotFound("کیف پول کاربر یافت نشد.");

            var requestId = WalletDebitRequestId.NewId();
            var amount = Money.Create(request.Amount, DefaultCurrency);

            wallet.CreateDebitRequest(
                requestId,
                amount,
                request.Reason,
                request.Description,
                adminId,
                TimeSpan.FromHours(request.ExpiryHours));

            walletRepository.Update(wallet);
            await unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<Guid>.Success(requestId.Value);
        }
        catch (InsufficientWalletBalanceException ex)
        {
            return ServiceResult<Guid>.Failure(ex.Message);
        }
        catch (WalletInactiveException)
        {
            return ServiceResult<Guid>.Failure("کیف پول کاربر مسدود است. ابتدا آن را رفع مسدودی کنید.");
        }
        catch (ConcurrencyException)
        {
            return ServiceResult<Guid>.Conflict("تعارض همزمانی رخ داد. لطفاً مجدداً تلاش کنید.");
        }
        catch (DomainException ex)
        {
            return ServiceResult<Guid>.Failure(ex.Message);
        }
    }
}
