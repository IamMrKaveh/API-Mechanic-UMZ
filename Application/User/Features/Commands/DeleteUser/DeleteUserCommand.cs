using Application.Common.Results;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.DeleteUser;

public record DeleteUserCommand(UserId Id, Guid CurrentUserId) : IRequest<ServiceResult>;