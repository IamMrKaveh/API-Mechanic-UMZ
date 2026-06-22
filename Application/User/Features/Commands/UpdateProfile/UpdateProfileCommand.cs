using Application.User.Features.Shared;

namespace Application.User.Features.Commands.UpdateProfile;

public record UpdateProfileCommand(
    string? FirstName,
    string? LastName)
    : ICommand<UserProfileDto>;