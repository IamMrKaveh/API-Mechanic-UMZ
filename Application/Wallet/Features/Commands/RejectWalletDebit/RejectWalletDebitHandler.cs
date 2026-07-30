using Domain.User.ValueObjects;
using Domain.Wallet.Exceptions;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Application.Wallet.Features.Commands.RejectWalletDebit;

public sealed class RejectWalletDebitHandler(
    IWalletDebitRequestRepository debitRequestRepository,
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    IDistributedLock distributedLock,
    ICurrentUserService currentUserService)
    : ICommandHandler<RejectWalletDebitCommand, Unit>
{
    private static readonly TimeSpan WalletLockExpiry = TimeSpan.FromSeconds(10);

    public async Task<ServiceResult<Unit>> Handle(RejectWalletDebitCommand request, CancellationToken ct)
    {
        var currentUserId = UserId.From(currentUserService.UserId.Value);
        var requestId = WalletDebitRequestId.From(request.RequestId);

        var debitRequest = await debitRequestRepository.GetByIdAsync(requestId, ct);
        if (debitRequest is null)
            return ServiceResult<Unit>.NotFound("درخواست کسر یافت نشد.");

        if (!debitRequest.OwnerId.Equals(currentUserId))
            return ServiceResult<Unit>.Forbidden("شما مجاز به رد این درخواست نیستید.");

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

            wallet.RejectDebitRequest(requestId, currentUserId, request.RejectionReason?.Trim());
            walletRepository.Update(wallet);
            await unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<Unit>.Success(Unit.Value);
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
