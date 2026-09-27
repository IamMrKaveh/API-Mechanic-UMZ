using Domain.User.ValueObjects;
using Domain.Wallet.Exceptions;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Wallet.Features.Commands.RejectWalletDebit;

public sealed class RejectWalletDebitHandler(
    IWalletDebitRequestRepository debitRequestRepository,
    IWalletRepository walletRepository,
    IDistributedLock distributedLock,
    IDateTimeProvider dateTimeProvider,
    ICurrentUserService currentUserService)
    : ICommandHandler<RejectWalletDebitCommand, Unit>
{
    private static readonly TimeSpan WalletLockExpiry = TimeSpan.FromSeconds(10);

    public async Task<ServiceResult<Unit>> Handle(RejectWalletDebitCommand request, CancellationToken ct)
    {
        var currentUserId = UserId.From(currentUserService.UserId!.Value);
        var requestId = WalletDebitRequestId.From(request.RequestId);

        var debitRequestResult = await (debitRequestRepository.GetByIdAsync(requestId, ct)).OrNotFoundAsync("درخواست کسر یافت نشد.");
        if (debitRequestResult.IsFailure) return debitRequestResult.Error;
        var debitRequest = debitRequestResult.Value;

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
            var walletResult = await (walletRepository.GetByUserIdForUpdateAsync(debitRequest.OwnerId, ct)).OrNotFoundAsync("کیف پول یافت نشد.");
            if (walletResult.IsFailure) return walletResult.Error;
            var wallet = walletResult.Value;

            var now = dateTimeProvider.UtcNow;
            wallet.RejectDebitRequest(requestId, currentUserId, request.RejectionReason?.Trim(), now);
            walletRepository.Update(wallet);

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
