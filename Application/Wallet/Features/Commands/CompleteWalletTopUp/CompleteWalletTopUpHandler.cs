using Domain.Wallet.Enums;
using Domain.Wallet.Interfaces;

namespace Application.Wallet.Features.Commands.CompleteWalletTopUp;

public sealed class CompleteWalletTopUpHandler(
    IWalletTopUpRepository topUpRepository,
    IWalletRepository walletRepository,
    IPaymentGatewayFactory gatewayFactory,
    IUnitOfWork unitOfWork,
    IAuditService auditService)
    : IRequestHandler<CompleteWalletTopUpCommand, ServiceResult<CompleteWalletTopUpResult>>
{
    public async Task<ServiceResult<CompleteWalletTopUpResult>> Handle(
        CompleteWalletTopUpCommand request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Authority))
        {
            return ServiceResult<CompleteWalletTopUpResult>.Success(
                new CompleteWalletTopUpResult(null, false, "invalid",
                    "پارامترهای بازگشتی از درگاه معتبر نیستند."));
        }

        var topUp = await topUpRepository.GetByAuthorityAsync(request.Authority, ct);
        if (topUp is null)
        {
            return ServiceResult<CompleteWalletTopUpResult>.Success(
                new CompleteWalletTopUpResult(null, false, "not_found",
                    "درخواست شارژ یافت نشد."));
        }

        if (topUp.Status != WalletTopUpStatus.Pending)
        {
            var isSuccessAlready = topUp.Status == WalletTopUpStatus.Succeeded;
            return ServiceResult<CompleteWalletTopUpResult>.Success(
                new CompleteWalletTopUpResult(
                    topUp.Id.Value,
                    isSuccessAlready,
                    topUp.Status.ToString().ToLowerInvariant(),
                    topUp.FailureReason,
                    topUp.Amount.Amount,
                    topUp.GatewayRefId));
        }

        try
        {
            if (!string.Equals(request.Status, "OK", StringComparison.OrdinalIgnoreCase))
            {
                topUp.MarkCancelled("پرداخت توسط کاربر لغو شد.");
                topUpRepository.Update(topUp);
                await unitOfWork.SaveChangesAsync(ct);
                return ServiceResult<CompleteWalletTopUpResult>.Success(
                    new CompleteWalletTopUpResult(topUp.Id.Value, false, "cancelled",
                        topUp.FailureReason, topUp.Amount.Amount));
            }

            var gateway = gatewayFactory.GetGateway(topUp.Gateway);
            var verifyResult = await gateway.VerifyAsync(request.Authority, topUp.Amount, ct);

            if (verifyResult.IsFailed || verifyResult.Value is null || !verifyResult.Value.IsVerified)
            {
                var reason = verifyResult.Error ?? "تأیید تراکنش با شکست مواجه شد.";
                topUp.MarkFailed(reason);
                topUpRepository.Update(topUp);
                await unitOfWork.SaveChangesAsync(ct);
                return ServiceResult<CompleteWalletTopUpResult>.Success(
                    new CompleteWalletTopUpResult(topUp.Id.Value, false, "failed",
                        reason, topUp.Amount.Amount));
            }

            var refId = verifyResult.Value.RefId?.ToString() ?? request.Authority;
            topUp.MarkSucceeded(refId);

            var wallet = await walletRepository.GetByUserIdForUpdateAsync(topUp.UserId, ct);
            if (wallet is null)
            {
                wallet = Domain.Wallet.Aggregates.Wallet.Create(topUp.UserId);
                await walletRepository.AddAsync(wallet, ct);
            }

            wallet.Credit(
                topUp.Amount,
                $"شارژ کیف پول - شماره پیگیری: {refId}",
                topUp.Id.Value.ToString());

            topUpRepository.Update(topUp);
            walletRepository.Update(wallet);
            await unitOfWork.SaveChangesAsync(ct);

            await auditService.LogSystemEventAsync(
                "WalletTopUpSucceeded",
                $"TopUpId={topUp.Id.Value}, Amount={topUp.Amount.Amount}, RefId={refId}",
                ct);

            return ServiceResult<CompleteWalletTopUpResult>.Success(
                new CompleteWalletTopUpResult(
                    topUp.Id.Value,
                    true,
                    "succeeded",
                    null,
                    topUp.Amount.Amount,
                    refId));
        }
        catch (DomainException ex)
        {
            return ServiceResult<CompleteWalletTopUpResult>.Failure(ex.Message);
        }
    }
}