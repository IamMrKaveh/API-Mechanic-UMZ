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
    IOptions<ZarinPalOptions> zarinPalOptions,
    ICurrentUserService currentUserService) : IPaymentService
{
    private readonly ZarinPalOptions _zarinPalOptions = zarinPalOptions.Value;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<ServiceResult<PaymentInitiationResult>> InitiatePaymentAsync(
        OrderId orderId,
        Money amount,
        IpAddress ipAddress,
        UserId userId,
        string? gatewayName = null,
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
            var startPayBase = _zarinPalOptions.UseSandbox
                ? (_zarinPalOptions.SandboxStartPayBaseUrl ?? string.Empty).TrimEnd('/')
                : _zarinPalOptions.StartPayBaseUrl.TrimEnd('/');
            var url = $"{startPayBase}/{existing.Authority.Value}";
            return ServiceResult<PaymentInitiationResult>.Success(
                new PaymentInitiationResult(existing.Authority.Value, url, existing.Id.Value));
        }

        IPaymentGateway gateway;
        try
        {
            gateway = gatewayFactory.GetGateway(gatewayName ?? string.Empty);
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult<PaymentInitiationResult>.Failure(ex.Message);
        }

        var callbackUrl = $"{_currentUserService.FrontendBaseUrl}/payment/callback";

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
            orderId, userId, initiateResult.Value!.Authority,
            amount.Amount, gateway.GatewayName, dateTimeProvider.UtcNow);

        await paymentRepository.AddAsync(transaction, ct);

        if (order.Status == OrderStatusValue.Created)
            order.MoveToPending();

        orderRepository.Update(order);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult<PaymentInitiationResult>.Success(
            new PaymentInitiationResult(
                initiateResult.Value!.Authority,
                initiateResult.Value!.PaymentUrl,
                transaction.Id.Value));
    }

    public async Task<ServiceResult<PaymentVerificationResult>> VerifyPaymentAsync(
        string authority, CancellationToken ct = default)
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
                new PaymentVerificationResult(
                    transaction.Id.Value, true, transaction.RefId, null, transaction.Fee));
        }

        if (!transaction.CanBeVerified(now))
            return ServiceResult<PaymentVerificationResult>.Failure("تراکنش قابل تأیید نیست.");

        var gateway = gatewayFactory.GetGateway(transaction.Gateway.Value);
        var verifyResult = await gateway.VerifyAsync(authority, transaction.Amount, ct);

        if (!verifyResult.IsSuccess)
        {
            await auditService.LogWarningAsync(
                $"[Payment] Gateway communication failure for authority {authority}: {verifyResult.Error}", ct);
            return ServiceResult<PaymentVerificationResult>.Failure(
                verifyResult.Error ?? "ارتباط با درگاه پرداخت برقرار نشد. لطفاً مجدداً تلاش کنید.");
        }

        if (!verifyResult.Value!.IsVerified)
        {
            transaction.MarkAsFailed(now, verifyResult.Error ?? "تأیید پرداخت توسط درگاه ناموفق بود.");
            paymentRepository.Update(transaction);
            await unitOfWork.SaveChangesAsync(ct);
            return ServiceResult<PaymentVerificationResult>.Failure(
                verifyResult.Error ?? "پرداخت تأیید نشد.");
        }

        var verifiedRefId = verifyResult.Value!.RefId ?? 0L;
        if (verifiedRefId <= 0)
        {
            await auditService.LogWarningAsync(
                $"[Payment] Gateway reported verified but RefId is invalid for authority {authority}.", ct);
            return ServiceResult<PaymentVerificationResult>.Failure("پاسخ درگاه پرداخت معتبر نیست. لطفاً مجدداً تلاش کنید.");
        }

        transaction.MarkAsSuccess(verifiedRefId, now, verifyResult.Value!.Fee);
        paymentRepository.Update(transaction);

        if (!order.IsPaid)
            order.MarkAsPaid(transaction.Id);

        orderRepository.Update(order);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult<PaymentVerificationResult>.Success(
            new PaymentVerificationResult(
                transaction.Id.Value, true, verifiedRefId,
                verifyResult.Value!.CardPan, verifyResult.Value!.Fee));
    }

    public async Task<ServiceResult> ProcessWebhookAsync(
        string authority, string status, CancellationToken ct = default)
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

        if (transaction.IsPending())
        {
            transaction.MarkAsFailed(now, $"Webhook status: {status}");
            paymentRepository.Update(transaction);
            await unitOfWork.SaveChangesAsync(ct);
        }

        return ServiceResult.Success();
    }
}