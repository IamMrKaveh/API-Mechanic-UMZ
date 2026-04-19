using Application.Communication.Contracts;
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
            var url = $"https://api.kavenegar.com/v1/{_options.ApiKey}/verify/lookup.json";
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["receptor"] = phoneNumber.Value,
                ["token"] = code.Value,
                ["template"] = _options.OtpTemplate
            });

            var response = await httpClient.PostAsync(url, content, ct);
            response.EnsureSuccessStatusCode();

            return true;
        }
        catch (Exception ex)
        {
            await auditService.LogErrorAsync($"[SMS] Failed to send OTP to {phoneNumber.Value}: {ex.Message}", ct);
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