using Application.Common.Results;
using Application.User.Features.Shared;

namespace Application.User.Features.Commands.UpdateProfile;

public record UpdateProfileCommand(
    Guid UserId,
    string? FirstName,
    string? LastName,
    string? PhoneNumber) : IRequest<ServiceResult<UserProfileDto>>;