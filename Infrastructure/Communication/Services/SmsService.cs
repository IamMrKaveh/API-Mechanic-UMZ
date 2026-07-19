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
        var maskedReceptor = MaskPhoneNumber(phoneNumber.Value);

        try
        {
            var url = $"https://api.kavenegar.com/v1/{_options.ApiKey}/verify/lookup.json";

            var form = new List<KeyValuePair<string, string>>
            {
                new("receptor", phoneNumber.Value),
                new("token", code.Value),
                new("template", _options.OtpTemplate)
            };

            using var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, url)
            {
                Content = new FormUrlEncodedContent(form)
            };

            var response = await httpClient.SendAsync(request, ct);

            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                await auditService.LogErrorAsync(
                    $"[SMS] Kavenegar HTTP error {(int)response.StatusCode} for {maskedReceptor}.",
                    ct);

                return false;
            }

            using var document = JsonDocument.Parse(responseBody);

            if (!document.RootElement.TryGetProperty("return", out var returnElement))
            {
                await auditService.LogErrorAsync(
                    $"[SMS] Invalid Kavenegar response for {maskedReceptor}.",
                    ct);

                return false;
            }

            var status = returnElement.GetProperty("status").GetInt32();

            if (status is not 200)
            {
                var message = returnElement.GetProperty("message").GetString();

                await auditService.LogErrorAsync(
                    $"[SMS] Kavenegar API error {status}: {message} | receptor: {maskedReceptor}",
                    ct);

                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            await auditService.LogErrorAsync(
                $"[SMS] Failed to send OTP to {maskedReceptor}: {ex.GetType().Name}: {ex.Message}",
                ct);

            return false;
        }
    }

    private static string MaskPhoneNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        if (value.Length <= 4)
            return new string('*', value.Length);

        return $"{value[..2]}{new string('*', value.Length - 4)}{value[^2..]}";
    }
}
