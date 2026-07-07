using Domain.Wallet.Interfaces;

namespace Application.Wallet.Features.Commands.CompleteWalletTopUp;

public sealed class CompleteWalletTopUpHandler(
    IWalletTopUpRepository topUpRepository,
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
            return ServiceResult<CompleteWalletTopUpResult>.Success(
                new CompleteWalletTopUpResult(null, false, "invalid", "پارامترهای بازگشتی از درگاه معتبر نیستند."));

        var topUp = await topUpRepository.GetByAuthorityAsync(request.Authority, ct);
        if (topUp is null)
            return ServiceResult<CompleteWalletTopUpResult>.Success(
                new CompleteWalletTopUpResult(null, false, "not_found", "درخواست شارژ یافت نشد."));

        if (topUp.Status != Domain.Wallet.Enums.WalletTopUpStatus.Pending)
            return ServiceResult<CompleteWalletTopUpResult>.Success(
                new CompleteWalletTopUpResult(
                    topUp.Id.Value,
                    topUp.Status == Domain.Wallet.Enums.WalletTopUpStatus.Succeeded,
                    topUp.Status.ToString().ToLowerInvariant(),
                    topUp.FailureReason));

        try
        {
            if (!string.Equals(request.Status, "OK", StringComparison.OrdinalIgnoreCase))
            {
                topUp.MarkCancelled("پرداخت توسط کاربر لغو یا نامعتبر بازگشت داده شد.");
                topUpRepository.Update(topUp);
                await unitOfWork.SaveChangesAsync(ct);

                return ServiceResult<CompleteWalletTopUpResult>.Success(
                    new CompleteWalletTopUpResult(topUp.Id.Value, false, "cancelled", topUp.FailureReason));
            }

            var gateway = gatewayFactory.GetGateway(topUp.Gateway);
            var verifyResult = await gateway.VerifyAsync(request.Authority, topUp.Amount, ct);

            if (verifyResult.IsFailed || verifyResult.Value is null || !verifyResult.Value.IsVerified)
            {
                var reason = verifyResult.Error ?? "تایید تراکنش با شکست مواجه شد.";
                topUp.MarkFailed(reason);
                topUpRepository.Update(topUp);
                await unitOfWork.SaveChangesAsync(ct);

                await auditService.LogWarningAsync(
                    $"WalletTopUp verification failed. TopUpId={topUp.Id}, Reason={reason}",
                    ct);

                return ServiceResult<CompleteWalletTopUpResult>.Success(
                    new CompleteWalletTopUpResult(topUp.Id.Value, false, "failed", reason));
            }

            var refId = verifyResult.Value.RefId?.ToString() ?? request.Authority;
            topUp.MarkSucceeded(refId);
            topUpRepository.Update(topUp);
            await unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<CompleteWalletTopUpResult>.Success(
                new CompleteWalletTopUpResult(topUp.Id.Value, true, "succeeded", null));
        }
        catch (DomainException ex)
        {
            return ServiceResult<CompleteWalletTopUpResult>.Failure(ex.Message);
        }
    }
}