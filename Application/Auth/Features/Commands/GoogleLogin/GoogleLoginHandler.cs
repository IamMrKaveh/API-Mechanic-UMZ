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

        if (user is null)
        {
            user = Domain.User.Aggregates.User.Create(
                FullName.Create(request.FirstName, request.LastName),
                email,
                null,
                null);

            user.UpdateProfile(
                FullName.Create(request.FirstName, request.LastName),
                null);

            await userRepository.AddAsync(user, ct);
        }

        var ipAddress = IpAddress.Create(currentUser.IpAddress ?? IpAddress.Unknown.Value);

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