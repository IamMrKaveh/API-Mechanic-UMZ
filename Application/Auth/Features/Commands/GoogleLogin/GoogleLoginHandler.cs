using Application.Auth.Features.Shared;
using Domain.Security.ValueObjects;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.Auth.Features.Commands.GoogleLogin;

public class GoogleLoginHandler(
    IUserRepository userRepository,
    IJwtTokenGenerator jwtTokenGenerator,
    ISessionService sessionService,
    ICurrentUserService currentUser)
    : ICommandHandler<GoogleLoginCommand, TokenResultDto>
{
    public async Task<ServiceResult<TokenResultDto>> Handle(GoogleLoginCommand request, CancellationToken ct)
    {
        var email = Email.Create(request.Email);
        var user = await userRepository.GetByEmailAsync(email, ct);

        // Locked-out accounts must not obtain a session through another login path.
        if (user is not null && user.IsLockedOut)
            return ServiceResult<TokenResultDto>.Forbidden(
                "حساب کاربری به دلیل تلاش‌های ناموفق متعدد قفل شده است. لطفاً بعداً تلاش کنید.");

        if (user is null)
        {
            user = Domain.User.Aggregates.User.CreateExternal(
                FullName.Create(request.FirstName, request.LastName),
                email,
                phoneNumber: null);

            await userRepository.AddAsync(user, ct);
        }

        var rawIp = currentUser.IpAddress;
        var ipAddress = string.IsNullOrWhiteSpace(rawIp)
            ? IpAddress.Unknown
            : IpAddress.Create(rawIp);

        var sessionResult = await sessionService.CreateSessionAsync(
            user.Id,
            ipAddress,
            currentUser.UserAgent,
            ct);

        if (sessionResult.IsSuccess is false)
            return ServiceResult<TokenResultDto>.Failure(sessionResult.Error);

        var session = sessionResult.Value!;
        var sessionId = SessionId.From(session.SessionId);
        var accessToken = jwtTokenGenerator.GenerateAccessToken(user, sessionId);

        return ServiceResult<TokenResultDto>.Success(new TokenResultDto(accessToken, session.RefreshToken));
    }
}
