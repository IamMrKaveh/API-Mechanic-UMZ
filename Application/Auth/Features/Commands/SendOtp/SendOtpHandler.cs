using Application.Auth.Contracts;
using Application.Common.Results;

namespace Application.Auth.Features.Commands.SendOtp;

public class SendOtpHandler(
    IAuthService authService,
    ILogger<SendOtpHandler> logger) : IRequestHandler<SendOtpCommand, ServiceResult>
{
    private readonly IAuthService _authService = authService;
    private readonly ILogger<SendOtpHandler> _logger = logger;

    public async Task<ServiceResult> Handle(
        SendOtpCommand request,
        CancellationToken ct)
    {
        _logger.LogInformation("OTP request for {Phone}", request.PhoneNumber);
        return await _authService.RequestOtpAsync(request.PhoneNumber, request.IpAddress, ct);
    }
}