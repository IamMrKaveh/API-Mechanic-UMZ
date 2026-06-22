using Application.Payment.Contracts;
using Application.Payment.Features.Shared;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.Payment.Aggregates;
using Domain.Payment.Interfaces;
using Domain.User.ValueObjects;
using Infrastructure.Payment.ZarinPal.Options;

namespace Infrastructure.Payment.Services;

public sealed class PaymentService(
    IPaymentTransactionRepository paymentRepository,
    IOrderRepository orderRepository,
    IPaymentGatewayFactory gatewayFactory,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService,
    IOptions<ZarinPalOptions> zarinPalOptions) : IPaymentService
{
    private readonly ZarinPalOptions _zarinPalOptions = zarinPalOptions.Value;

    public async Task<ServiceResult<PaymentInitiationResult>> InitiatePaymentAsync(
        OrderId orderId,
        Money amount,
        IpAddress ipAddress,
        UserId userId,
        CancellationToken ct = default)
    {
        var order = await orderRepository.FindByIdAsync(orderId, ct);
        if (order is null)
            return ServiceResult<PaymentInitiationResult>.NotFound("سفارش یافت نشد.");

        if (order.IsPaid)
            return ServiceResult<PaymentInitiationResult>.Conflict("سفارش قبلاً پرداخت شده است.");

        var existing = await paymentRepository.GetActiveByOrderIdAsync(orderId, ct);
        if (existing is not null)
        {
            var gw = gatewayFactory.GetGateway();
            var startPayBase = _zarinPalOptions.UseSandbox
                ? _zarinPalOptions.SandboxStartPayBaseUrl.TrimEnd('/')
                : _zarinPalOptions.StartPayBaseUrl.TrimEnd('/');
            var url = $"{startPayBase}/{existing.Authority.Value}";
            return ServiceResult<PaymentInitiationResult>.Success(
                new PaymentInitiationResult(existing.Authority.Value, url));
        }

        var gateway = gatewayFactory.GetGateway();
        var callbackUrl = _zarinPalOptions.ApiBaseUrl;

        var initiateResult = await gateway.InitiateAsync(
            orderId,
            amount,
            $"پرداخت سفارش {order.OrderNumber.Value}",
            callbackUrl,
            ct: ct);

        if (!initiateResult.IsSuccess)
        {
            await auditService.LogErrorAsync(
                $"[Payment] Gateway initiation failed for Order {orderId.Value}: {initiateResult.Error}", ct);
            return ServiceResult<PaymentInitiationResult>.Failure(
                initiateResult.Error ?? "خطا در اتصال به درگاه پرداخت.");
        }

        var transaction = PaymentTransaction.Initiate(
            orderId,
            userId,
            initiateResult.Value!.Authority,
            amount.Amount,
            gateway.GatewayName,
            dateTimeProvider.UtcNow);

        await paymentRepository.AddAsync(transaction, ct);

        if (order.Status == OrderStatusValue.Created)
            order.MoveToPending();

        orderRepository.Update(order);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult<PaymentInitiationResult>.Success(initiateResult.Value!);
    }

    public async Task<ServiceResult<PaymentVerificationResult>> VerifyPaymentAsync(
        string authority,
        CancellationToken ct = default)
    {
        var now = dateTimeProvider.UtcNow;

        var transaction = await paymentRepository.GetByAuthorityAsync(authority, ct);
        if (transaction is null)
            return ServiceResult<PaymentVerificationResult>.NotFound("تراکنش پیدا نشد.");

        var order = await orderRepository.FindByIdAsync(transaction.OrderId, ct);
        if (order is null)
            return ServiceResult<PaymentVerificationResult>.NotFound("سفارش یافت نشد.");

        if (transaction.IsSuccessful())
        {
            if (!order.IsPaid)
            {
                order.MarkAsPaid(transaction.Id);
                orderRepository.Update(order);
                await unitOfWork.SaveChangesAsync(ct);
            }

            return ServiceResult<PaymentVerificationResult>.Success(
                new PaymentVerificationResult(transaction.Id.Value, true, transaction.RefId, null, transaction.Fee));
        }

        if (!transaction.CanBeVerified(now))
            return ServiceResult<PaymentVerificationResult>.Failure("تراکنش قابل تأیید نیست.");

        var gateway = gatewayFactory.GetGateway(transaction.Gateway.Value);
        var verifyResult = await gateway.VerifyAsync(authority, transaction.Amount, ct);

        if (!verifyResult.IsSuccess || !verifyResult.Value!.IsVerified)
        {
            transaction.MarkAsFailed(now, verifyResult.Error ?? "تأیید ناموفق");
            paymentRepository.Update(transaction);
            await unitOfWork.SaveChangesAsync(ct);
            return ServiceResult<PaymentVerificationResult>.Failure(
                verifyResult.Error ?? "پرداخت تأیید نشد.");
        }

        transaction.MarkAsSuccess(verifyResult.Value!.RefId!.Value, now, verifyResult.Value!.Fee);
        paymentRepository.Update(transaction);

        if (!order.IsPaid)
            order.MarkAsPaid(transaction.Id);

        orderRepository.Update(order);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult<PaymentVerificationResult>.Success(verifyResult.Value!);
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
            return verifyResult.IsSuccess
                ? ServiceResult.Success()
                : ServiceResult.Failure(verifyResult.Error ?? "خطا");
        }

        transaction.MarkAsFailed(now, $"Webhook status: {status}");
        paymentRepository.Update(transaction);
        await unitOfWork.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }
}