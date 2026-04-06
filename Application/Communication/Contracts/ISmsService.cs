namespace Application.Communication.Contracts;

public interface ISmsService
{
    Task<bool> SendOtpAsync(string phoneNumber, string code, CancellationToken ct = default);

    Task<bool> SendAsync(string phoneNumber, string message, CancellationToken ct = default);

    Task<bool> SendTemplateAsync(string phoneNumber, string templateName, Dictionary<string, string> parameters, CancellationToken ct = default);
}