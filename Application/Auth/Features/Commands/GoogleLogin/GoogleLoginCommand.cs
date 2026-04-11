using Application.Auth.Features.Shared;

namespace Application.Auth.Features.Commands.GoogleLogin;

public record GoogleLoginCommand(
    string Email,
    string FirstName,
    string LastName,
    string ProviderKey) : IRequest<ServiceResult<TokenResultDto>>;