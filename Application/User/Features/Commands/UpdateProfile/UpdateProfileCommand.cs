namespace Application.User.Features.Commands.UpdateProfile;

public record UpdateProfileCommand : IRequest<ServiceResult<UserProfileDto>>
{
    public int UserId { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
}