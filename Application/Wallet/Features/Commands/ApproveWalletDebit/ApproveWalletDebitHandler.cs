using Domain.User.ValueObjects;
using Domain.Wallet.Exceptions;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Application.Wallet.Features.Commands.ApproveWalletDebit;

public sealed class ApproveWalletDebitHandler(
    IWalletDebitRequestRepository debitRequestRepository,
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    IDistributedLock distributedLock,
    ICurrentUserService currentUserService)
    : ICommandHandler<ApproveWalletDebitCommand, Unit>
{
    private static readonly TimeSpan WalletLockExpiry = TimeSpan.FromSeconds(10);

    public async Task<ServiceResult<Unit>> Handle(ApproveWalletDebitCommand request, CancellationToken ct)
    {
        var currentUserId = UserId.From(currentUserService.UserId.Value);
        var requestId = WalletDebitRequestId.From(request.RequestId);

        var debitRequest = await debitRequestRepository.GetByIdAsync(requestId, ct);
        if (debitRequest is null)
            return ServiceResult<Unit>.NotFound("درخواست کسر یافت نشد.");

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
            var wallet = await walletRepository.GetByUserIdForUpdateAsync(debitRequest.OwnerId, ct);
            if (wallet is null)
                return ServiceResult<Unit>.NotFound("کیف پول یافت نشد.");

            wallet.ApproveDebitRequest(requestId, currentUserId);
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
