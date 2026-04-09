using Application.Common.Results;
using Application.User.Features.Shared;

namespace Application.User.Features.Commands.CreateUser;

public record CreateUserCommand(
    string PhoneNumber,
    string? FirstName,
    string? LastName,
    string? Email,
    bool IsAdmin = false) : IRequest<ServiceResult<UserProfileDto>>;