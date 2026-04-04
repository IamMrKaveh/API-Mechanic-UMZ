using Application.Common.Results;
using Application.User.Features.Shared;

namespace Application.User.Features.Commands.CreateUser;

public record CreateUserCommand(AdminCreateUserDto Dto) : IRequest<ServiceResult<UserProfileDto>>;