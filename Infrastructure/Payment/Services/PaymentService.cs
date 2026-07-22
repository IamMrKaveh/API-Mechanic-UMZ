using Application.Payment.Contracts;
using Application.Payment.Features.Shared;
using Domain.Order.Exceptions;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.Payment.Aggregates;
using Domain.Payment.Exceptions;
using Domain.Payment.Interfaces;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Payment.ZarinPal.Options;
using Microsoft.FeatureManagement;
using SharedContracts.FeatureManagement;
using SharedKernel.Exceptions;

namespace Infrastructure.Payment.Services;

public sealed class PaymentService(
    IPaymentTransactionRepository paymentRepository,
    IOrderRepository orderRepository,
    IPaymentGatewayFactory gatewayFactory,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService,
    IOptions<ZarinPalOptions> zarinPalOptions,
    ICurrentUserService currentUserService,
    IPaymentCallbackNonceService callbackNonceService,
    IFeatureManager featureManager) : IPaymentService
{
    private static readonly TimeSpan NonceTtl = TimeSpan.FromMinutes(30);
    private readonly ZarinPalOptions _zarinPalOptions = zarinPalOptions.Value;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<PaymentInitiationResult> InitiatePaymentAsync(
        OrderId orderId,
        Money amount,
        IpAddress ipAddress,
        UserId userId,
        string? gatewayName = null,
        CancellationToken ct = default)
    {
        var order = await orderRepository.FindByIdAsync(orderId, ct)
            ?? throw new OrderNotFoundException(orderId);

        if (order.IsPaid)
            throw new OrderAlreadyPaidException(orderId);

        var existing = await paymentRepository.GetActiveByOrderIdAsync(orderId, ct);
        if (existing is not null)
        {
            var startPayBase = _zarinPalOptions.UseSandbox
                ? (_zarinPalOptions.SandboxStartPayBaseUrl ?? string.Empty).TrimEnd('/')
                : _zarinPalOptions.StartPayBaseUrl.TrimEnd('/');
            var url = $"{startPayBase}/{existing.Authority.Value}";
            return new PaymentInitiationResult(existing.Authority.Value, url, existing.Id.Value);
        }

        IPaymentGateway gateway;
        try
        {
            gateway = gatewayFactory.GetGateway(gatewayName ?? string.Empty);
        }
        catch (InvalidOperationException ex)
        {
            throw new ExternalServiceException("PaymentGatewayFactory", ex.Message, ex);
        }

        var paymentTransactionId = PaymentTransactionId.NewId();

        var nonce = await callbackNonceService.IssueAsync(paymentTransactionId.Value, NonceTtl, ct);
        var callbackUrl = BuildCallbackUrl(paymentTransactionId.Value, nonce);

        PaymentInitiationResult gatewayResult;
        try
        {
            gatewayResult = await gateway.InitiateAsync(
                orderId,
                amount,
                $"پرداخت سفارش {order.OrderNumber.Value}",
                callbackUrl,
                ct: ct);
        }
        catch (ExternalServiceException ex)
        {
            await auditService.LogErrorAsync(
                $"[Payment] Gateway initiation failed for Order {orderId.Value}: {ex.Message}", ct);
            throw;
        }

        var transaction = PaymentTransaction.InitiateWithId(
            paymentTransactionId,
            orderId,
            userId,
            gatewayResult.Authority,
            amount.Amount,
            gateway.GatewayName,
            dateTimeProvider.UtcNow);

        await paymentRepository.AddAsync(transaction, ct);

        if (order.Status == OrderStatusValue.Created)
            order.MoveToPending();

        orderRepository.Update(order);
        await unitOfWork.SaveChangesAsync(ct);

        return new PaymentInitiationResult(
            gatewayResult.Authority,
            gatewayResult.PaymentUrl,
            transaction.Id.Value);
    }

    public async Task<PaymentVerificationResult> VerifyPaymentAsync(
        string authority, CancellationToken ct = default)
    {
        var now = dateTimeProvider.UtcNow;

        var transaction = await paymentRepository.GetByAuthorityAsync(authority, ct)
            ?? throw new PaymentTransactionNotFoundException(authority);

        var order = await orderRepository.FindByIdAsync(transaction.OrderId, ct)
            ?? throw new OrderNotFoundException(transaction.OrderId);

        if (transaction.IsSuccessful())
        {
            if (!order.IsPaid)
            {
                order.MarkAsPaid(transaction.Id);
                orderRepository.Update(order);
                await unitOfWork.SaveChangesAsync(ct);
            }

            return new PaymentVerificationResult(
                transaction.Id.Value, true, transaction.RefId, null, transaction.Fee);
        }

        if (!transaction.CanBeVerified(now))
            throw new PaymentNotVerifiableException(transaction.Authority);

        var gateway = gatewayFactory.GetGateway(transaction.Gateway.Value);

        PaymentVerificationResult gatewayResult;
        try
        {
            gatewayResult = await gateway.VerifyAsync(authority, transaction.Amount, ct);
        }
        catch (ExternalServiceException ex)
        {
            await auditService.LogWarningAsync(
                $"[Payment] Gateway communication failure for authority {authority}: {ex.Message}", ct);
            throw;
        }

        var verifiedRefId = gatewayResult.RefId ?? 0L;
        if (verifiedRefId <= 0)
        {
            await auditService.LogWarningAsync(
                $"[Payment] Gateway reported verified but RefId is invalid for authority {authority}.", ct);
            throw new ExternalServiceException(
                gateway.GatewayName,
                "پاسخ درگاه پرداخت معتبر نیست. لطفاً مجدداً تلاش کنید.");
        }

        transaction.MarkAsSuccess(verifiedRefId, now, gatewayResult.Fee);
        paymentRepository.Update(transaction);

        if (!order.IsPaid)
            order.MarkAsPaid(transaction.Id);

        orderRepository.Update(order);
        await unitOfWork.SaveChangesAsync(ct);

        return new PaymentVerificationResult(
            transaction.Id.Value, true, verifiedRefId,
            gatewayResult.CardPan, gatewayResult.Fee);
    }

    public async Task ProcessWebhookAsync(
        string authority,
        string status,
        string? nonce = null,
        CancellationToken ct = default)
    {
        var now = dateTimeProvider.UtcNow;

        var transaction = await paymentRepository.GetByAuthorityAsync(authority, ct)
            ?? throw new PaymentTransactionNotFoundException(authority);

        var signatureRequired = await featureManager.IsEnabledAsync(
            FeatureFlags.PaymentCallbackSignatureRequired);

        if (signatureRequired)
        {
            var nonceValid = await callbackNonceService.ValidateAndConsumeAsync(
                transaction.Id.Value, nonce ?? string.Empty, ct);

            if (!nonceValid)
            {
                await auditService.LogSecurityEventAsync(
                    "PaymentWebhookInvalidNonce",
                    $"Rejected webhook for transaction {transaction.Id.Value} (authority '{authority}') due to invalid or missing nonce.",
                    IpAddress.Unknown,
                    null,
                    ct);
                throw new ExternalServiceException(
                    transaction.Gateway.Value,
                    "درخواست بازگشتی درگاه پرداخت معتبر نیست.");
            }
        }

        if (status.Equals("OK", StringComparison.OrdinalIgnoreCase))
        {
            await VerifyPaymentAsync(authority, ct);
            return;
        }

        if (transaction.IsPending())
        {
            transaction.MarkAsFailed(now, $"Webhook status: {status}");
            paymentRepository.Update(transaction);
            await unitOfWork.SaveChangesAsync(ct);
        }
    }

    private string BuildCallbackUrl(Guid paymentTransactionId, string nonce)
    {
        var baseUrl = _currentUserService.FrontendBaseUrl?.TrimEnd('/') ?? string.Empty;
        return $"{baseUrl}/payment/callback?paymentId={paymentTransactionId:N}&nonce={Uri.EscapeDataString(nonce)}";
    }
}
