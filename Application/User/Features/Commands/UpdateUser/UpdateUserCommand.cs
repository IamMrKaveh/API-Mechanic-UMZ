using Application.Common.Results;
using Application.User.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.UpdateUser;

public record UpdateUserCommand(UserId Id, UpdateProfileDto UpdateRequest, Guid CurrentUserId) : IRequest<ServiceResult>;