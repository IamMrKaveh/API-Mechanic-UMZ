using Application.Common.Results;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.ChangeUserStatus;

public record ChangeUserStatusCommand(UserId Id, bool IsActive) : IRequest<ServiceResult>;