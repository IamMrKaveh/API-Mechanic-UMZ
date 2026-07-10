using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Application.Wallet.Features.Commands.ApproveWithdrawal;

public sealed class ApproveWithdrawalHandler(
    IWalletWithdrawalRepository withdrawalRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService)
    : ICommandHandler<ApproveWithdrawalCommand, Unit>
{
    public async Task<ServiceResult<Unit>> Handle(
        ApproveWithdrawalCommand request,
        CancellationToken ct)
    {
        try
        {
            var withdrawalId = WalletWithdrawalRequestId.From(request.WithdrawalId);
            var adminId = UserId.From(request.AdminId);

            var withdrawal = await withdrawalRepository.GetByIdForUpdateAsync(withdrawalId, ct);
            if (withdrawal is null)
                return ServiceResult<Unit>.NotFound("درخواست برداشت یافت نشد.");

            withdrawal.Approve(adminId);
            withdrawalRepository.Update(withdrawal);
            await unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (ConcurrencyException)
        {
            await auditService.LogSystemEventAsync(
                "WithdrawalApproveConcurrencyConflict",
                $"تعارض همزمانی در تأیید درخواست برداشت {request.WithdrawalId}",
                ct);
            return ServiceResult<Unit>.Conflict("تعارض همزمانی رخ داد. لطفاً مجدداً تلاش کنید.");
        }
        catch (DomainException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
    }
}