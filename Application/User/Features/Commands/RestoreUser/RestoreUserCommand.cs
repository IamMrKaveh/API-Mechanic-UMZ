using Application.Common.Models;

namespace Application.User.Features.Commands.RestoreUser;

public record RestoreUserCommand(int Id) : IRequest<ServiceResult>;