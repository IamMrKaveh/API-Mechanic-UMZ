using Application.Payment.Contracts;
using Application.Payment.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.Payment.Aggregates;
using Domain.Payment.Interfaces;

namespace Infrastructure.Payment.Services;

public sealed class PaymentService(
    IPaymentTransactionRepository paymentRepository,
    IPaymentGatewayFactory gatewayFactory,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService) : IPaymentService
{
    public async Task<ServiceResult<PaymentInitiationResult>> InitiatePaymentAsync(
        OrderId orderId,
        Money amount,
        IpAddress ipAddress,
        CancellationToken ct = default)
    {
        var existingPending = await paymentRepository.GetActiveByOrderIdAsync(orderId, ct);
        if (existingPending is not null)
        {
            await auditService.LogWarningAsync(
                $"[Payment] Duplicate initiation attempt for Order {orderId.Value}. Returning existing.", ct);
            return ServiceResult<PaymentInitiationResult>.Success(
                new PaymentInitiationResult(existingPending.Authority.Value, string.Empty));
        }

        var gateway = gatewayFactory.GetGateway();
        var initiateResult = await gateway.InitiateAsync(
            orderId, amount,
            $"پرداخت سفارش {orderId.Value}",
            $"/payment/callback",
            ct: ct);

        if (!initiateResult.IsSuccess)
        {
            await auditService.LogErrorAsync(
                $"[Payment] Gateway initiation failed for Order {orderId.Value}", ct);
            return ServiceResult<PaymentInitiationResult>.Failure(
                initiateResult.Error ?? "خطا در اتصال به درگاه پرداخت.");
        }

        var transaction = PaymentTransaction.Initiate(
            orderId,
            Domain.User.ValueObjects.UserId.NewId(),
            initiateResult.Value.Authority,
            amount.Amount,
            gateway.GatewayName,
            dateTimeProvider.UtcNow);

        await paymentRepository.AddAsync(transaction, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult<PaymentInitiationResult>.Success(initiateResult.Value);
    }

    public async Task<ServiceResult<PaymentVerificationResult>> VerifyPaymentAsync(
        string authority,
        CancellationToken ct = default)
    {
        var now = dateTimeProvider.UtcNow;

        var transaction = await paymentRepository.GetByAuthorityAsync(authority, ct);
        if (transaction is null)
            return ServiceResult<PaymentVerificationResult>.NotFound("تراکنش پیدا نشد.");

        if (transaction.IsSuccessful())
        {
            await auditService.LogWarningAsync(
                $"[Payment] Duplicate verify for authority {authority}. Already verified.", ct);
            return ServiceResult<PaymentVerificationResult>.Success(
                new PaymentVerificationResult(transaction.Id.Value, true, transaction.RefId, null, transaction.Fee));
        }

        if (!transaction.CanBeVerified(now))
            return ServiceResult<PaymentVerificationResult>.Failure("تراکنش قابل تأیید نیست.");

        var gateway = gatewayFactory.GetGateway(transaction.Gateway.Value);
        var verifyResult = await gateway.VerifyAsync(authority, transaction.Amount, ct);

        if (!verifyResult.IsSuccess || !verifyResult.Value.IsVerified)
        {
            transaction.MarkAsFailed(now, verifyResult.Error ?? "تأیید ناموفق");
            paymentRepository.Update(transaction);
            await unitOfWork.SaveChangesAsync(ct);
            return ServiceResult<PaymentVerificationResult>.Failure(
                verifyResult.Error ?? "پرداخت تأیید نشد.");
        }

        transaction.MarkAsSuccess(verifyResult.Value.RefId!.Value, now, verifyResult.Value.Fee);
        paymentRepository.Update(transaction);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult<PaymentVerificationResult>.Success(verifyResult.Value);
    }

    public async Task<ServiceResult> ProcessWebhookAsync(
        string authority,
        string status,
        CancellationToken ct = default)
    {
        var now = dateTimeProvider.UtcNow;

        var transaction = await paymentRepository.GetByAuthorityAsync(authority, ct);
        if (transaction is null)
            return ServiceResult.NotFound("تراکنش پیدا نشد.");

        if (status.Equals("OK", StringComparison.OrdinalIgnoreCase))
        {
            var verifyResult = await VerifyPaymentAsync(authority, ct);
            return verifyResult.IsSuccess ? ServiceResult.Success() : ServiceResult.Failure(verifyResult.Error ?? "خطا");
        }

        transaction.MarkAsFailed(now, $"Webhook status: {status}");
        paymentRepository.Update(transaction);
        await unitOfWork.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }
}