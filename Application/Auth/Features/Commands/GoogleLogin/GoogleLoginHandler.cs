using Application.Auth.Features.Shared;
using Domain.Common.ValueObjects;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.Auth.Features.Commands.GoogleLogin;

public class GoogleLoginHandler(
    IUserRepository userRepository,
    IJwtTokenGenerator jwtTokenGenerator,
    IUnitOfWork unitOfWork) : IRequestHandler<GoogleLoginCommand, ServiceResult<TokenResultDto>>
{
    public async Task<ServiceResult<TokenResultDto>> Handle(GoogleLoginCommand request, CancellationToken ct)
    {
        var email = Email.Create(request.Email);
        var user = await userRepository.GetByEmailAsync(email, ct);

        if (user is null)
        {
            user = Domain.User.Aggregates.User.Create(
                UserId.NewId(),
                FullName.Create(request.FirstName, request.LastName),
                email,
                null,
                null);

            user.UpdateProfile(
                FullName.Create(request.FirstName, request.LastName),
                null);

            await userRepository.AddAsync(user, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }

        var (accessToken, refreshToken) = jwtTokenGenerator.GenerateTokens(user);

        return ServiceResult<TokenResultDto>.Success(new TokenResultDto(accessToken, refreshToken));
    }
}