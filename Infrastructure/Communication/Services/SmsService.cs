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

    public async Task<bool> SendSMSAsync(
        PhoneNumber phoneNumber,
        string message,
        CancellationToken ct = default)
    {
        try
        {
            var url = $"https://api.kavenegar.com/v1/{_options.ApiKey}/sms/send.json";
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["receptor"] = phoneNumber.Value,
                ["sender"] = _options.Sender,
                ["message"] = message
            });

            var response = await httpClient.PostAsync(url, content, ct);
            response.EnsureSuccessStatusCode();

            return true;
        }
        catch (Exception ex)
        {
            await auditService.LogErrorAsync($"[SMS] Failed to send SMS to {phoneNumber.Value}: {ex.Message}", ct);
            return false;
        }
    }

    public async Task<bool> SendTemplateSMSAsync(
        PhoneNumber phoneNumber,
        string templateName,
        Dictionary<string, string> parameters,
        CancellationToken ct = default)
    {
        try
        {
            var url = $"https://api.kavenegar.com/v1/{_options.ApiKey}/verify/lookup.json";

            var formData = new Dictionary<string, string>
            {
                ["receptor"] = phoneNumber.Value,
                ["template"] = templateName
            };

            var tokenIndex = 1;
            foreach (var (_, value) in parameters)
            {
                var tokenKey = tokenIndex == 1 ? "token" : $"token{tokenIndex}";
                formData[tokenKey] = value;
                tokenIndex++;
            }

            var content = new FormUrlEncodedContent(formData);
            var response = await httpClient.PostAsync(url, content, ct);
            response.EnsureSuccessStatusCode();

            return true;
        }
        catch (Exception ex)
        {
            await auditService.LogErrorAsync($"[SMS] Failed to send template SMS to {phoneNumber.Value}: {ex.Message}", ct);
            return false;
        }
    }
}