using Application.Payment.Contracts;
using Application.Payment.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Payment.ZarinPal.Options;
using SharedKernel.Exceptions;

namespace Infrastructure.Payment.ZarinPal;

public sealed class ZarinPalPaymentGateway(
    HttpClient httpClient,
    IOptions<ZarinPalOptions> options,
    IAuditService auditService) : IPaymentGateway
{
    private readonly ZarinPalOptions _options = options.Value;

    public string GatewayName => "Zarinpal";

    public async Task<PaymentInitiationResult> InitiateAsync(
        OrderId orderId,
        Money amount,
        string description,
        string callbackUrl,
        Email? email = null,
        PhoneNumber? phoneNumber = null,
        CancellationToken ct = default)
    {
        ZarinPalRequestResponse? result;
        try
        {
            var request = new
            {
                merchant_id = _options.MerchantId,
                amount = ToRial(amount),
                description,
                callback_url = callbackUrl,
                metadata = new
                {
                    mobile = phoneNumber?.Value,
                    email = email?.Value,
                    order_id = orderId.Value.ToString()
                }
            };

            using var response = await httpClient.PostAsJsonAsync("request.json", request, ct);
            result = await response.Content.ReadFromJsonAsync<ZarinPalRequestResponse>(cancellationToken: ct);
        }
        catch (Exception ex) when (ex is not ExternalServiceException and not OperationCanceledException)
        {
            await auditService.LogErrorAsync($"[ZarinPal] Initiate exception: {ex.Message}", ct);
            throw new ExternalServiceException(GatewayName, "خطا در ارتباط با درگاه زرین‌پال.", ex);
        }

        if (result?.Data?.Code == 100 && !string.IsNullOrWhiteSpace(result.Data.Authority))
        {
            var paymentUrl = $"{_options.StartPayBaseUrl.TrimEnd('/')}/{result.Data.Authority}";
            return new PaymentInitiationResult(result.Data.Authority, paymentUrl, Guid.Empty);
        }

        var code = result?.Errors?.Code ?? -1;
        await auditService.LogErrorAsync($"[ZarinPal] Request failed code={code}", ct);
        throw new ExternalServiceException(GatewayName, ZarinPalErrorMapper.GetMessage(code), code.ToString());
    }

    public async Task<PaymentVerificationResult> VerifyAsync(
        string authority,
        Money expectedAmount,
        CancellationToken ct = default)
    {
        ZarinPalVerifyResponse? result;
        try
        {
            var request = new
            {
                merchant_id = _options.MerchantId,
                amount = ToRial(expectedAmount),
                authority
            };

            using var response = await httpClient.PostAsJsonAsync("verify.json", request, ct);
            result = await response.Content.ReadFromJsonAsync<ZarinPalVerifyResponse>(cancellationToken: ct);
        }
        catch (Exception ex) when (ex is not ExternalServiceException and not OperationCanceledException)
        {
            await auditService.LogErrorAsync($"[ZarinPal] Verify exception: {ex.Message}", ct);
            throw new ExternalServiceException(GatewayName, "خطا در تأیید پرداخت زرین‌پال.", ex);
        }

        if (result?.Data?.Code is 100 or 101)
        {
            return new PaymentVerificationResult(null, true, result.Data.RefId, result.Data.CardPan, result.Data.Fee);
        }

        var code = result?.Errors?.Code ?? -1;
        throw new ExternalServiceException(GatewayName, ZarinPalErrorMapper.GetMessage(code), code.ToString());
    }

    private static long ToRial(Money amount)
    {
        var value = amount.Amount;
        if (string.Equals(amount.Currency, "IRT", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(amount.Currency, "TOMAN", StringComparison.OrdinalIgnoreCase))
            value *= 10m;
        return (long)Math.Round(value, MidpointRounding.AwayFromZero);
    }
}

internal sealed class ZarinPalRequestResponse
{ public ZarinPalRequestData? Data { get; init; } public ZarinPalError? Errors { get; init; } }

internal sealed class ZarinPalRequestData
{ public int Code { get; init; } public string? Authority { get; init; } public string? Message { get; init; } }

internal sealed class ZarinPalVerifyResponse
{ public ZarinPalVerifyData? Data { get; init; } public ZarinPalError? Errors { get; init; } }

internal sealed class ZarinPalVerifyData
{ public int Code { get; init; } public long RefId { get; init; } public string? CardPan { get; init; } public decimal Fee { get; init; } public string? Message { get; init; } }

internal sealed class ZarinPalError
{ public int Code { get; init; } public string? Message { get; init; } }