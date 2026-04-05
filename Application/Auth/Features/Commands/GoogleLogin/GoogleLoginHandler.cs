using Application.Auth.Contracts;
using Application.Auth.Features.Shared;
using Application.Common.Results;
using Domain.Common.Interfaces;
using Domain.User.Interfaces;

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
        var user = await _userRepository.GetByEmailAsync(request.Email, ct);

        if (user == null)
        {
            user = Domain.User.Aggregates.User.Create(string.Empty);
            user.UpdateProfile(
                Domain.User.ValueObjects.FullName.Create(request.FirstName, request.LastName).Value,
                null);
            user.SetEmail(request.Email);

            await _userRepository.AddAsync(user, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        var (accessToken, refreshToken) = _jwtTokenGenerator.GenerateTokens(user);

        return ServiceResult<TokenResultDto>.Success(new TokenResultDto(accessToken, refreshToken));
    }
}