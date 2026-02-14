namespace Application.User.Features.Commands.CreateUser;

public record CreateUserCommand(Domain.User.User User) : IRequest<ServiceResult<(UserProfileDto? User, string? Error)>>;