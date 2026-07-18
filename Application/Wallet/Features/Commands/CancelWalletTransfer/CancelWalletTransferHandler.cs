using Domain.User.ValueObjects;
using Domain.Wallet.Exceptions;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Application.Wallet.Features.Commands.CancelWalletTransfer;

public sealed class CancelWalletTransferHandler(
    IWalletTransferRepository transferRepository,
    ICurrentUserService currentUserService)
    : IRequestHandler<CancelWalletTransferCommand, ServiceResult<Unit>>
{
    public async Task<ServiceResult<Unit>> Handle(
        CancelWalletTransferCommand request,
        CancellationToken ct)
    {
        try
        {
            var transferId = WalletTransferId.From(request.TransferId);
            var fromUserId = UserId.From(currentUserService.UserId.Value);

            var transfer = await transferRepository.GetByIdForUpdateAsync(transferId, ct);
            if (transfer is null)
                return ServiceResult<Unit>.NotFound("درخواست انتقال یافت نشد.");

            if (!transfer.FromUserId.Equals(fromUserId))
                return ServiceResult<Unit>.Forbidden("دسترسی به این درخواست انتقال مجاز نیست.");

            transfer.Cancel(fromUserId);
            transferRepository.Update(transfer);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (InvalidWalletTransferException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
        catch (DomainException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
    }
}