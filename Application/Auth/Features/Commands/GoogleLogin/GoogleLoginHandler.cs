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
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator = jwtTokenGenerator;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ServiceResult<TokenResultDto>> Handle(GoogleLoginCommand request, CancellationToken ct)
    {
        var email = Email.Create(request.Email);
        var user = await _userRepository.GetByEmailAsync(email, ct);

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

            await _userRepository.AddAsync(user, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        var (accessToken, refreshToken) = _jwtTokenGenerator.GenerateTokens(user);

        return ServiceResult<TokenResultDto>.Success(new TokenResultDto(accessToken, refreshToken));
    }
}