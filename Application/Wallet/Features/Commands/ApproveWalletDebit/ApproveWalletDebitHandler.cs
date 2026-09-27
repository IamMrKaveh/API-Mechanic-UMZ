using Domain.User.ValueObjects;
using Domain.Wallet.Exceptions;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Wallet.Features.Commands.ApproveWalletDebit;

public sealed class ApproveWalletDebitHandler(
    IWalletDebitRequestRepository debitRequestRepository,
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    IDistributedLock distributedLock,
    IDateTimeProvider dateTimeProvider,
    ICurrentUserService currentUserService)
    : ICommandHandler<ApproveWalletDebitCommand, Unit>
{
    private static readonly TimeSpan WalletLockExpiry = TimeSpan.FromSeconds(10);

    public async Task<ServiceResult<Unit>> Handle(ApproveWalletDebitCommand request, CancellationToken ct)
    {
        var currentUserId = UserId.From(currentUserService.UserId!.Value);
        var requestId = WalletDebitRequestId.From(request.RequestId);

        var debitRequestResult = await (debitRequestRepository.GetByIdAsync(requestId, ct)).OrNotFoundAsync("درخواست کسر یافت نشد.");
        if (debitRequestResult.IsFailure) return debitRequestResult.Error;
        var debitRequest = debitRequestResult.Value;

        if (!debitRequest.OwnerId.Equals(currentUserId))
            return ServiceResult<Unit>.Forbidden("شما مجاز به تایید این درخواست نیستید.");

        await using var lockHandle = await distributedLock.AcquireAsync(
            $"wallet:{debitRequest.OwnerId.Value:N}",
            WalletLockExpiry,
            ct);

        if (lockHandle is null || !lockHandle.IsAcquired)
            return ServiceResult<Unit>.Conflict("عملیات دیگری روی کیف پول در حال انجام است.");

        try
        {
            var walletResult = await (walletRepository.GetByUserIdForUpdateAsync(debitRequest.OwnerId, ct)).OrNotFoundAsync("کیف پول یافت نشد.");
            if (walletResult.IsFailure) return walletResult.Error;
            var wallet = walletResult.Value;

            var now = dateTimeProvider.UtcNow;
            wallet.ApproveDebitRequest(requestId, currentUserId, now);
            walletRepository.Update(wallet);
            await unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (WalletDebitRequestExpiredException)
        {
            return ServiceResult<Unit>.Failure("مهلت پاسخ به این درخواست به پایان رسیده است.");
        }
        catch (InvalidWalletDebitRequestStatusException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
        catch (DomainException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
    }
}
