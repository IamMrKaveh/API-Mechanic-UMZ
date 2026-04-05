using Application.Auth.Features.Shared;
using Application.Common.Results;

namespace Application.Auth.Features.Commands.GoogleLogin;

public record GoogleLoginCommand(
    string Email,
    string FirstName,
    string LastName,
    string ProviderKey) : IRequest<ServiceResult<TokenResultDto>>;