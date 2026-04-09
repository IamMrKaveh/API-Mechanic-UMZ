namespace Application.User.Features.Commands.UpdateUser;

public record UpdateUserCommand(
    Guid Id,
    Guid CurrentUserId,
    string FirstName,
    string LastName,
    string? Email) : IRequest<ServiceResult>;