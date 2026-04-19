using Application.Payment.Contracts;
using Application.Payment.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Payment.ZarinPal.Options;
using Microsoft.Extensions.Options;

namespace Infrastructure.Payment.ZarinPal;

public sealed class ZarinPalPaymentGateway(
    HttpClient httpClient,
    IOptions<ZarinPalOptions> options,
    IAuditService auditService) : IPaymentGateway
{
    private readonly ZarinPalOptions _options = options.Value;

    public string GatewayName => "Zarinpal";

    public async Task<ServiceResult<PaymentInitiationResult>> InitiateAsync(
        OrderId orderId,
        Money amount,
        string description,
        string callbackUrl,
        Email? email = null,
        PhoneNumber? phoneNumber = null,
        CancellationToken ct = default)
    {
        try
        {
            var request = new
            {
                merchant_id = _options.MerchantId,
                amount = (long)amount.ToRialDecimal(),
                description,
                callback_url = callbackUrl,
                metadata = new
                {
                    mobile = phoneNumber?.Value,
                    email = email?.Value
                }
            };

            var response = await httpClient.PostAsJsonAsync(
                "https://api.zarinpal.com/pg/v4/payment/request.json", request, ct);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ZarinPalRequestResponse>(
                cancellationToken: ct);

            if (result?.Data?.Code == 100 && result.Data.Authority is not null)
            {
                var paymentUrl = $"https://www.zarinpal.com/pg/StartPay/{result.Data.Authority}";
                return ServiceResult<PaymentInitiationResult>.Success(
                    new PaymentInitiationResult(result.Data.Authority, paymentUrl));
            }

            var errorCode = result?.Errors?.Code ?? -1;
            var errorMsg = ZarinPalErrorMapper.GetMessage(errorCode);
            await auditService.LogErrorAsync($"[ZarinPal] Request failed: {errorMsg}", ct);
            return ServiceResult<PaymentInitiationResult>.Failure(errorMsg);
        }
        catch (Exception ex)
        {
            await auditService.LogErrorAsync($"[ZarinPal] Exception in InitiateAsync: {ex.Message}", ct);
            return ServiceResult<PaymentInitiationResult>.Failure("خطا در اتصال به درگاه زرین‌پال.");
        }
    }

    public async Task<ServiceResult<PaymentVerificationResult>> VerifyAsync(
        string authority,
        Money expectedAmount,
        CancellationToken ct = default)
    {
        try
        {
            var request = new
            {
                merchant_id = _options.MerchantId,
                amount = (long)expectedAmount.ToRialDecimal(),
                authority
            };

            var response = await httpClient.PostAsJsonAsync(
                "https://api.zarinpal.com/pg/v4/payment/verify.json", request, ct);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ZarinPalVerifyResponse>(
                cancellationToken: ct);

            if (result?.Data?.Code is 100 or 101)
            {
                return ServiceResult<PaymentVerificationResult>.Success(
                    new PaymentVerificationResult(
                        TransactionId: null,
                        IsVerified: true,
                        RefId: result.Data.RefId,
                        CardPan: result.Data.CardPan,
                        Fee: result.Data.Fee));
            }

            var errorCode = result?.Errors?.Code ?? -1;
            var errorMsg = ZarinPalErrorMapper.GetMessage(errorCode);
            return ServiceResult<PaymentVerificationResult>.Failure(errorMsg);
        }
        catch (Exception ex)
        {
            await auditService.LogErrorAsync($"[ZarinPal] Exception in VerifyAsync: {ex.Message}", ct);
            return ServiceResult<PaymentVerificationResult>.Failure("خطا در تأیید پرداخت زرین‌پال.");
        }
    }
}

internal sealed class ZarinPalRequestResponse
{
    public ZarinPalRequestData? Data { get; init; }
    public ZarinPalError? Errors { get; init; }
}

internal sealed class ZarinPalRequestData
{
    public int Code { get; init; }
    public string? Authority { get; init; }
    public string? Message { get; init; }
}

internal sealed class ZarinPalVerifyResponse
{
    public ZarinPalVerifyData? Data { get; init; }
    public ZarinPalError? Errors { get; init; }
}

internal sealed class ZarinPalVerifyData
{
    public int Code { get; init; }
    public long RefId { get; init; }
    public string? CardPan { get; init; }
    public string? CardHash { get; init; }
    public decimal Fee { get; init; }
    public string? Message { get; init; }
}

internal sealed class ZarinPalError
{
    public int Code { get; init; }
    public string? Message { get; init; }
}