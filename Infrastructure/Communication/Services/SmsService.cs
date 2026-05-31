using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Communication.Options;

namespace Infrastructure.Communication.Services;

public sealed class SmsService(
    HttpClient httpClient,
    IOptions<KavenegarOptions> options,
    IAuditService auditService) : ISmsService
{
    private readonly KavenegarOptions _options = options.Value;

    public async Task<bool> SendOtpSMSAsync(
    PhoneNumber phoneNumber,
    OtpCode code,
    CancellationToken ct = default)
    {
        try
        {
            var url =
                $"https://api.kavenegar.com/v1/{_options.ApiKey}/verify/lookup.json" +
                $"?receptor={Uri.EscapeDataString(phoneNumber.Value)}" +
                $"&token={Uri.EscapeDataString(code.Value)}" +
                $"&template={Uri.EscapeDataString(_options.OtpTemplate)}";

            using var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);

            var response = await httpClient.SendAsync(request, ct);

            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                await auditService.LogErrorAsync(
                    $"[SMS] Kavenegar HTTP error {(int)response.StatusCode} for {phoneNumber.Value}: {responseBody}",
                    ct);

                return false;
            }

            using var document = JsonDocument.Parse(responseBody);

            if (!document.RootElement.TryGetProperty("return", out var returnElement))
            {
                await auditService.LogErrorAsync(
                    $"[SMS] Invalid Kavenegar response for {phoneNumber.Value}: {responseBody}",
                    ct);

                return false;
            }

            var status = returnElement.GetProperty("status").GetInt32();

            if (status is not 200)
            {
                var message = returnElement.GetProperty("message").GetString();

                await auditService.LogErrorAsync(
                    $"[SMS] Kavenegar API error {status}: {message} | receptor: {phoneNumber.Value}",
                    ct);

                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            await auditService.LogErrorAsync(
                $"[SMS] Failed to send OTP to {phoneNumber.Value}: {ex}",
                ct);

            return false;
        }
    }
}