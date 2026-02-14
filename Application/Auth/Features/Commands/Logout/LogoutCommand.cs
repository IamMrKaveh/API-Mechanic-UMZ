namespace Application.Auth.Features.Commands.Logout;

public record LogoutCommand : IRequest<ServiceResult>
{
    public int UserId { get; init; }
    public string? RefreshToken { get; init; }
}