using Application.Auth.Contracts;
using Application.Auth.Features.Shared;
using Application.Common.Results;
using Domain.Common.ValueObjects;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Auth.Features.Commands.VerifyOtp;

public class VerifyOtpHandler(
    IAuthService authService,
    ILogger<VerifyOtpHandler> logger) : IRequestHandler<VerifyOtpCommand, ServiceResult<AuthResult>>
{
    public async Task<ServiceResult<AuthResult>> Handle(
        VerifyOtpCommand request,
        CancellationToken ct)
    {
        logger.LogInformation("OTP verify for {Phone}", request.PhoneNumber);

        var phoneNumberResult = PhoneNumber.TryCreate(request.PhoneNumber);
        if (phoneNumberResult.IsFailure)
            return ServiceResult<AuthResult>.Validation(phoneNumberResult.Error.Message);

        var otpCode = OtpCode.Create(request.Code);
        var ipAddress = IpAddress.Create(request.IpAddress);

        var result = await authService.VerifyOtpAsync(
            phoneNumberResult.Value,
            otpCode,
            ipAddress,
            request.UserAgent,
            ct);

        if (result.IsFailed)
            return ServiceResult<AuthResult>.Failure(result.Error ?? "");

        var (accessToken, refreshToken, user, isNewUser) = result.Value;

        return ServiceResult<AuthResult>.Success(new AuthResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.FullToken,
            User = user,
            IsNewUser = isNewUser
        });
    }
}