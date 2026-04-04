using Application.Common.Results;
using Application.User.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.UpdateProfile;

public record UpdateProfileCommand(
    UserId UserId,
    FullName? FullName,
    PhoneNumber? PhoneNumber,
    string? Email) : IRequest<ServiceResult<UserProfileDto>>;