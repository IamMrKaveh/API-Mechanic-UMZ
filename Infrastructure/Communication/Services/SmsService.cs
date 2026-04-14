using Application.Communication.Contracts;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Communication.Options;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Communication.Services;

public sealed class SmsService(
    IOptions<KavenegarOptions> options,
    ILogger<SmsService> logger) : ISmsService
{
    private readonly KavenegarOptions _options = options.Value;

    public async Task<bool> SendOtpSMSAsync(
        PhoneNumber phoneNumber,
        OtpCode code,
        CancellationToken ct = default)
    {
        try
        {
            using var client = new HttpClient();
            var url = $"https://api.kavenegar.com/v1/{_options.ApiKey}/verify/lookup.json";
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["receptor"] = phoneNumber.Value,
                ["token"] = code.Value,
                ["template"] = _options.OtpTemplate
            });

            var response = await client.PostAsync(url, content, ct);
            response.EnsureSuccessStatusCode();

            logger.LogInformation("OTP sent to {PhoneNumber}", phoneNumber.Value);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send OTP to {PhoneNumber}", phoneNumber.Value);
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
            using var client = new HttpClient();
            var url = $"https://api.kavenegar.com/v1/{_options.ApiKey}/sms/send.json";
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["receptor"] = phoneNumber.Value,
                ["sender"] = _options.Sender,
                ["message"] = message
            });

            var response = await client.PostAsync(url, content, ct);
            response.EnsureSuccessStatusCode();

            logger.LogInformation("SMS sent to {PhoneNumber}", phoneNumber.Value);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send SMS to {PhoneNumber}", phoneNumber.Value);
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
            using var client = new HttpClient();
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
            var response = await client.PostAsync(url, content, ct);
            response.EnsureSuccessStatusCode();

            logger.LogInformation("Template SMS sent to {PhoneNumber} with template {Template}", phoneNumber.Value, templateName);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send template SMS to {PhoneNumber}", phoneNumber.Value);
            return false;
        }
    }
}