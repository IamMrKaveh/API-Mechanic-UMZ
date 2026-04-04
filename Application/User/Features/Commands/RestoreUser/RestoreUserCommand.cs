using Application.Common.Results;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.RestoreUser;

public record RestoreUserCommand(UserId Id) : IRequest<ServiceResult>;