namespace Application.User.Features.Commands.CreateUser;

public record CreateUserCommand(AdminCreateUserDto Dto) : IRequest<ServiceResult<UserProfileDto>>;