using Application.Payment.Contracts;
using Application.Payment.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Payment.ZarinPal.Options;

namespace Infrastructure.Payment.ZarinPal;

public sealed class ZarinPalSandboxGateway(
    IOptions<ZarinPalOptions> options,
    IAuditService auditService) : IPaymentGateway
{
    private readonly ZarinPalOptions _options = options.Value;

    public string GatewayName => "ZarinpalSandbox";

    public async Task<ServiceResult<PaymentInitiationResult>> InitiateAsync(
        OrderId orderId,
        Money amount,
        string description,
        string callbackUrl,
        Email? email = null,
        PhoneNumber? phoneNumber = null,
        CancellationToken ct = default)
    {
        var authority = string.Concat("S", Guid.NewGuid().ToString("N").AsSpan(0, 35));
        var startPayBase = _options.SandboxStartPayBaseUrl.TrimEnd('/');
        var paymentUrl = $"{startPayBase}/{authority}";

        await auditService.LogInformationAsync(
            $"[ZarinPalSandbox] Initiated for Order {orderId.Value} amount {amount.Amount} authority {authority}", ct);

        return ServiceResult<PaymentInitiationResult>.Success(
            new PaymentInitiationResult(authority, paymentUrl));
    }

    public async Task<ServiceResult<PaymentVerificationResult>> VerifyAsync(
        string authority,
        Money expectedAmount,
        CancellationToken ct = default)
    {
        await auditService.LogInformationAsync(
            $"[ZarinPalSandbox] Verify success for authority {authority} amount {expectedAmount.Amount}",
            ct);

        var refId = DateTime.UtcNow.Ticks;
        var result = new PaymentVerificationResult(null, true, refId, "6037********0000", 0m);
        return ServiceResult<PaymentVerificationResult>.Success(result);
    }
}